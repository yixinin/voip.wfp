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
    public sealed unsafe class VideoStreamDecoder : IDisposable
    {
        private readonly AVCodecContext* _pCodecContext;
        private readonly AVCodecParserContext* _pParserContext;
        //private readonly AVFormatContext* _pFormatContext;
        //private readonly int _streamIndex;
        //private readonly AVFrame* _pFrame;
        //private readonly AVFrame* _receivedFrame;
        //private readonly AVPacket* _pPacket;

        public Queue<VideoH264Packet> videoQueue { get; set; }


        public unsafe VideoStreamDecoder(Queue<VideoH264Packet> q, AVHWDeviceType HWDeviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
        {
            videoQueue = q;

            AVCodec* codec = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);

            _pCodecContext = ffmpeg.avcodec_alloc_context3(codec);
            _pParserContext = ffmpeg.av_parser_init(27);

            if (HWDeviceType != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
            {
                ffmpeg.av_hwdevice_ctx_create(&_pCodecContext->hw_device_ctx, HWDeviceType, null, null, 0).ThrowExceptionIfError();
            }

            if (_pCodecContext->hw_device_ctx != null)
            {
                Debug.WriteLine("_pCodecContext->hw_device_ctx != null");
            }

            ffmpeg.avcodec_open2(_pCodecContext, codec, null);
            //if (ffmpeg.avcodec_open2(_pCodecContext, codec, null) >= 0)
            //_receivedFrame = ffmpeg.av_frame_alloc();

            CodecName = ffmpeg.avcodec_get_name(codec->id);
            FrameSize = new Size(_pCodecContext->width, _pCodecContext->height);
            PixelFormat = _pCodecContext->pix_fmt;
            if (PixelFormat == AVPixelFormat.AV_PIX_FMT_NONE)
            {
                PixelFormat = AVPixelFormat.AV_PIX_FMT_YUV420P;
            }

            //_pPacket = ffmpeg.av_packet_alloc();
            //_pFrame = ffmpeg.av_frame_alloc();
        }

        public string CodecName { get; }
        public Size FrameSize { get; set; }
        public AVPixelFormat PixelFormat
        {
            get;
            set;
        }

        public void Dispose()
        {
            //ffmpeg.av_frame_unref(_pFrame);
            //ffmpeg.av_free(_pFrame);

            //ffmpeg.av_packet_unref(_pPacket);
            //ffmpeg.av_free(_pPacket);

            ffmpeg.avcodec_close(_pCodecContext);
            //var pFormatContext = _pFormatContext;
            //ffmpeg.avformat_close_input(&pFormatContext);
        }


        public unsafe Int32 PutVideoStream(byte[] buffer)
        {
            var parserLen = 0;
            try
            {



                var totalLen = buffer.Length;

                while (parserLen < buffer.Length)
                {
                    fixed (byte* pBuffer = buffer)
                    {
                        AVPacket* pParsePacket = ffmpeg.av_packet_alloc();
                        
                        var n = ffmpeg.av_parser_parse2(
                            _pParserContext,
                            _pCodecContext,
                            &pParsePacket->data,
                            &pParsePacket->size,
                            (pBuffer + parserLen),
                            buffer.Length - parserLen,
                            ffmpeg.AV_NOPTS_VALUE,
                            ffmpeg.AV_NOPTS_VALUE,
                            ffmpeg.AV_NOPTS_VALUE);
                        parserLen += n;
                        var newBuffer = new byte[buffer.Length - n];
                        Array.Copy(buffer, n, newBuffer, 0, newBuffer.Length);
                        buffer = newBuffer;
                        //Debug.WriteLine(Util.GetBufferText())

                        //输出packet数据
                        var buf = new byte[pParsePacket->size]; 
                        for (var i = 0; i < buf.Length; i++)
                        {
                            var b = Marshal.ReadByte((IntPtr)pParsePacket->data, i);
                            buf[i] = b;
                        }
                        Debug.WriteLine(String.Format("{0}", Util.GetBufferText(buf)));

                        int ret = ffmpeg.avcodec_send_packet(_pCodecContext, pParsePacket);
                        if (ret != 0)
                        {
                            Debug.WriteLine(ret);
                        }
                    }
                }


                return -1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("PutVideoStream Ex:{0}", ex));
                return -1;
            }
        }
        public bool TryDecodeNextFrame(out AVFrame frame)
        {
            //ffmpeg.av_frame_unref(_pFrame);
            //ffmpeg.av_frame_unref(_receivedFrame);
            int error = 0;

            var _pFrame = ffmpeg.av_frame_alloc();
            var _receivedFrame = ffmpeg.av_frame_alloc();


            try
            {
                if (videoQueue.Count > 0)
                {
                    var p = videoQueue.Dequeue();
                    if (p != null)
                    {
                        error = PutVideoStream(p.Buffer);
                        if (error != 0)
                        {
                            error.ThrowExceptionIfError();
                            //Debug.WriteLine(string.Format("try put packet err: {0}", error));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("try put packet ex: {0}", ex));
            }





            //Debug.WriteLine("---------------------------------try get frame-----------------------------");

            var tryCount = 0;
            do
            {
                error = ffmpeg.avcodec_receive_frame(_pCodecContext, _pFrame);
                //if (error == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                //{
                //    Task.Delay(10).Wait();
                //}
                tryCount++;
            } while (error == ffmpeg.AVERROR(ffmpeg.EAGAIN) && tryCount < 100); //超过500ms则放弃


            if (error != 0)
            {
                frame = *_pFrame;
                return false;
            }


            if (_pCodecContext->hw_device_ctx != null)
            {
                if (FrameSize.Width == 0 && _pCodecContext->width != 0)
                {
                    FrameSize = new Size(_pCodecContext->width, _pCodecContext->height);
                }
                ffmpeg.av_hwframe_transfer_data(_receivedFrame, _pFrame, 0).ThrowExceptionIfError();
                frame = *_receivedFrame;
            }
            else
            {
                frame = *_pFrame;
            }
            return true;
        }



    }
}