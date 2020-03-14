using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Voip
{
   
    public unsafe class IOVideoStreamDecoder : IDisposable
    {
        private readonly AVCodecContext* _pCodecContext;
        private readonly AVFormatContext* _pFormatContext;
        private readonly AVIOContext* _pAVIOContext;
        //private readonly AVInputFormat* _avInputFormat;

        private readonly byte* mVideoBuffer;
        private int pIndex;
        private readonly int _streamIndex;
        private readonly AVFrame* _pFrame;
        private readonly AVFrame* _receivedFrame;
        private readonly AVPacket* _pPacket;
        //private ReadWriteCallback callback; 
        public static avio_alloc_context_read_packet_func callback;
        const int VIDEO_BUFFER_SIZE = 327680;
        public Queue<VideoH264Packet> VideoQueue { get; set; }

        public Queue<byte> streamQueue { get; set; }

        unsafe public IOVideoStreamDecoder(Queue<VideoH264Packet> videoQueue, AVHWDeviceType HWDeviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
        {
            VideoQueue = videoQueue;
            streamQueue = new Queue<byte>();

            callback = (avio_alloc_context_read_packet_func)read_packet;
            Task.Run(TransData);

            var avInputFormat = ffmpeg.av_find_input_format("H264");
            _pFormatContext = ffmpeg.avformat_alloc_context();

            mVideoBuffer = (byte*)ffmpeg.av_malloc(VIDEO_BUFFER_SIZE);



            _pAVIOContext = ffmpeg.avio_alloc_context(
                mVideoBuffer,
                VIDEO_BUFFER_SIZE,
                0,
                null,
                callback,
                null,
                null);
            _pFormatContext->pb = _pAVIOContext;
            GC.KeepAlive(callback);

            
            var pFormatContext = _pFormatContext;
            ffmpeg.avformat_open_input(&pFormatContext, "", avInputFormat, null).ThrowExceptionIfError();

            _receivedFrame = ffmpeg.av_frame_alloc();

            //ffmpeg.avformat_find_stream_info(_pFormatContext, null).ThrowExceptionIfError();
            AVCodec* codec = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
            //_streamIndex = ffmpeg.av_find_best_stream(_pFormatContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &codec, 0).ThrowExceptionIfError();
            _pCodecContext = ffmpeg.avcodec_alloc_context3(codec);
            if (HWDeviceType != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
            {
                ffmpeg.av_hwdevice_ctx_create(&_pCodecContext->hw_device_ctx, HWDeviceType, null, null, 0).ThrowExceptionIfError();
            }
            //ffmpeg.avcodec_parameters_to_context(_pCodecContext, _pFormatContext->streams[_streamIndex]->codecpar).ThrowExceptionIfError();
            //ffmpeg.avcodec_open2(_pCodecContext, codec, null).ThrowExceptionIfError();

            CodecName = ffmpeg.avcodec_get_name(codec->id);
            //FrameSize = new Size(_pCodecContext->width, _pCodecContext->height);
            FrameSize = new Size(320, 320);
            PixelFormat = AVPixelFormat.AV_PIX_FMT_YUV420P;

            _pPacket = ffmpeg.av_packet_alloc();
            _pFrame = ffmpeg.av_frame_alloc();

        }

        public string CodecName { get; }
        public Size FrameSize { get; }
        public AVPixelFormat PixelFormat { get; }

        public void Dispose()
        {
            ffmpeg.av_frame_unref(_pFrame);
            ffmpeg.av_free(_pFrame);

            ffmpeg.av_packet_unref(_pPacket);
            ffmpeg.av_free(_pPacket);

            ffmpeg.avcodec_close(_pCodecContext);
            var pFormatContext = _pFormatContext;
            ffmpeg.avformat_close_input(&pFormatContext);
        }


        public void TransData()
        {

            while (true)
            {
                if (VideoQueue.Count > 0)
                { 
                    var p = VideoQueue.Dequeue();
                    if (p != null)
                    {
                        foreach (var b in p.Buffer)
                        {
                            streamQueue.Enqueue(b);
                        }
                        //fixed (byte* buf = p.Buffer)
                        //{
                        //    //ffmpeg.avio_write(_pAVIOContext, buf, p.Buffer.Length); 

                        //}

                        //Marshal.Copy(p.Buffer, 0, (IntPtr)this.mVideoBuffer, p.Buffer.Length);
                        //this.pIndex += p.Buffer.Length;
                    }

                }

            }
        }

        unsafe public int read_packet(void* opaque, byte* buf, int buf_size)
        {
            if (buf == null)
            {
                return -1;
            }
            var length = buf_size;
            var sc = streamQueue.Count;
            if (length > sc)
            {
                length = sc;
            }
            for (var i = 0; i < length; i++)
            {
                var b = streamQueue.Dequeue();
                Marshal.WriteByte((IntPtr)buf, i, b);
            }


            //AVIOContext* ctx = (AVIOContext*)opaque;
            //if (ctx != null)
            //{
            //    ffmpeg.avio_write(ctx, buf, buf_size);
            //}

            return length;
        }

        unsafe public int write_packet(void* opaque, byte* buf, int buf_size)
        {
            return 0;
        }
        public bool TryDecodeNextFrame(out AVFrame frame)
        {
            ffmpeg.av_frame_unref(_pFrame);
            ffmpeg.av_frame_unref(_receivedFrame);
            int error;
            var loop = 0;
            do
            {
                try
                {
                    do
                    {
                        error = ffmpeg.av_read_frame(_pFormatContext, _pPacket);
                        if (error == ffmpeg.AVERROR_EOF)
                        {
                            frame = *_pFrame;
                            return false;
                        }

                        error.ThrowExceptionIfError();
                    } while (_pPacket->stream_index != _streamIndex);

                    if (error == 0)
                    {
                        //输出packet数据
                        var buf = new byte[_pPacket->size];

                        for (var i = 0; i < buf.Length; i++)
                        {
                            var b = Marshal.ReadByte((IntPtr)_pPacket->data, i);
                            buf[i] = b;
                        }
                        Debug.WriteLine(String.Format("{0}", GetBufferText(buf)));


                        ffmpeg.avcodec_send_packet(_pCodecContext, _pPacket);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
                finally
                {
                    ffmpeg.av_packet_unref(_pPacket);
                }

                error = ffmpeg.avcodec_receive_frame(_pCodecContext, _pFrame);
                loop++;
                if (loop > 1000)
                {
                    frame = new AVFrame();
                    return false;
                }
            } while (error == ffmpeg.AVERROR(ffmpeg.EAGAIN));
            error.ThrowExceptionIfError();
            if (_pCodecContext->hw_device_ctx != null)
            {
                ffmpeg.av_hwframe_transfer_data(_receivedFrame, _pFrame, 0).ThrowExceptionIfError();
                frame = *_receivedFrame;
            }
            else
            {
                frame = *_pFrame;
            }

            return true;
        }


        public static string GetBufferText(byte[] buf)
        {
            return string.Format("[{0}]", string.Join(" ", buf));
        }
        public IReadOnlyDictionary<string, string> GetContextInfo()
        {
            AVDictionaryEntry* tag = null;
            var result = new Dictionary<string, string>();
            while ((tag = ffmpeg.av_dict_get(_pFormatContext->metadata, "", tag, ffmpeg.AV_DICT_IGNORE_SUFFIX)) != null)
            {
                var key = Marshal.PtrToStringAnsi((IntPtr)tag->key);
                var value = Marshal.PtrToStringAnsi((IntPtr)tag->value);
                result.Add(key, value);
            }

            return result;
        }
    }
}
