using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Voip
{
    public unsafe class HiDecoder
    {
        private readonly AVCodecContext* _pCodecContext;
        private readonly AVFormatContext* _pFormatContext;
        private readonly int _streamIndex;
        private readonly AVFrame* _pFrame;
        private readonly AVFrame* _receivedFrame;
        private readonly AVPacket* _pPacket;
        private readonly AVCodec* _pH264VideoDecoder;
        private readonly AVFrame* _pFrameYuv;
        public HiDecoder()
        {
            _pCodecContext = ffmpeg.avcodec_alloc_context3(null);
            _pH264VideoDecoder = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
            if (_pH264VideoDecoder == null)
            {
                return;
            }

            _pCodecContext->time_base.num = 1;
            _pCodecContext->frame_number = 1; //每包一个视频帧  
            _pCodecContext->codec_type = AVMediaType.AVMEDIA_TYPE_VIDEO;
            _pCodecContext->bit_rate = 0;
            _pCodecContext->time_base.den = 25;//帧率  
            _pCodecContext->width = 0;//视频宽  
            _pCodecContext->height = 0;//视频高 
            _pCodecContext->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUVJ420P;
            _pCodecContext->color_range = AVColorRange.AVCOL_RANGE_MPEG;

            if (ffmpeg.avcodec_open2(_pCodecContext, _pH264VideoDecoder, null) >= 0)
                _pFrameYuv = ffmpeg.av_frame_alloc();
        }

        public unsafe Int32 PutVideoStream(byte[] buffer)
        {
            AVPacket packet = new AVPacket();
            fixed (byte* pBuffer = buffer)
            {
                packet.data = pBuffer;    //这里填入一个指向完整H264数据帧的指针 
            }
            packet.size = buffer.Length;        //这个填入H264数据帧的大小  
            int ret = ffmpeg.avcodec_send_packet(_pCodecContext, &packet);
            return ret;
        }
        public Int32 GetNextVideoFrame(byte[] buf, Int32 bufferLen, Int32 yuFormate)
        {
            if (ffmpeg.avcodec_receive_frame(_pCodecContext, _pFrameYuv) == 0)
            {
                int height = _pCodecContext->height;
                int width = _pCodecContext->width;

                if (yuFormate == 1)
                {
                    ////写入数据  
                    int yLen = height * width;
                    //ffmpeg.memcpy(buffer, _pFrameYuv->data[0], yLen);
                    byte[] yData = new byte[yLen];
                    Marshal.Copy((IntPtr)_pFrameYuv->data[0], yData, 0, yLen);

                    Array.Copy(yData, 0, buf, 0, yLen);

                    int uLen = yLen / 4;
                    //ffmpeg.memcpy(buffer + yLen, _pFrameYuv->data[1], uLen);
                    byte[] uData = new byte[uLen];
                    Marshal.Copy((IntPtr)_pFrameYuv->data[1], uData, 0, uLen);
                    Array.Copy(uData, 0, buf, yLen, uLen);

                    int vLen = uLen;
                    //ffmpeg.memcpy(buffer + yLen + uLen, _pFrameYuv->data[2], vLen);
                    byte[] vData = new byte[vLen];
                    Marshal.Copy((IntPtr)_pFrameYuv->data[2], vData, 0, vLen);
                    Array.Copy(vData, 0, buf, yLen + uLen, vLen);
                    return 0;
                }
                else
                {
                    ////写入数据  
                    int yLen = height * width;
                    //ffmpeg.memcpy(buffer, _pFrameYuv->data[0], yLen);
                    byte[] yData = new byte[yLen];
                    Marshal.Copy((IntPtr)_pFrameYuv->data[0], yData, 0, yLen);

                    Array.Copy(yData, 0, buf, 0, yLen);

                    int uLen = yLen / 4;
                    //memcpy(buffer + yLen, _pFrameYuv->data[2], uLen);
                    byte[] uData = new byte[yLen];
                    Marshal.Copy((IntPtr)_pFrameYuv->data[2], uData, 0, uLen);
                    Array.Copy(uData, 0, buf, yLen, uLen);

                    int vLen = uLen;
                    //memcpy(buffer + yLen + uLen, _pFrameYuv->data[1], vLen);
                    byte[] vData = new byte[vLen];
                    Marshal.Copy((IntPtr)_pFrameYuv->data[1], vData, 0, vLen);
                    Array.Copy(vData, 0, buf, yLen + uLen, vLen);
                    return 0;
                }
            }
            return -1;
        }

        Int32 GetNextVideoFrame_Rgb(byte[] buf, Int32 bufferLen, Int32 width, Int32 height)
        {
            //if (ffmpeg.avcodec_receive_frame(_pCodecContext, _pFrameYuv) == 0)
            //{
            //   //ffmpeg.ResetRgbScale(width, height);

            //    int n = (ffmpeg._out_rgb_buffer_len == bufferLen);

            //    uint8_t* data[3];
            //    data[0] = _pFrameYuv->data[0];
            //    data[1] = _pFrameYuv->data[2]; //u v 向量互换
            //    data[2] = _pFrameYuv->data[1];

            //    _dst_dataTmp[0] = (uint8_t*)buffer; //少一次复制
            //    int ret = sws_scale(_img_convert_ctx, (const unsigned char* const*)data, _pFrameYuv->linesize, 0, _pCodecContext->height,
            //_dst_dataTmp, _dst_linesize);
            //    return 0;
            //}
            return -1;
        }
    }
}
