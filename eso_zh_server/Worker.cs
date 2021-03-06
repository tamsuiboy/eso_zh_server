﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;

namespace eso_zh_server
{
    class Worker
    {
        private Translator translator;

        public delegate void UpdateTextDelegate(String text);
        public event UpdateTextDelegate UpdateText;

        public String AppKey
        {
            get
            {
                lock (_AppKey)
                {
                    return _AppKey;
                }
            }
            set
            {
                lock (_AppKey)
                {
                    _AppKey = value;
                }
            }
        }
        private String _AppKey = "";

        FormConfig frm;
        String rawText;

        public Worker(FormConfig frm)
        {
            this.frm = frm;
            translator = new Translator();
        }

        public void Run()
        {
            int gc_count = 0;
            while (true)
            {
                try
                {
                    Thread.Sleep(1000);
                }
                catch (ThreadInterruptedException) // ok
                {
                    return;
                }
                try
                {
                    if (++gc_count > 10)    // sometimes automatic garbage collection does not work
                    {
                        GC.Collect();
                        gc_count = 0;
                    }
                    // capture & decode
                    String newRawText = getTextFromScreenQrCode();
                    // translate & display
                    if (newRawText != null && rawText != newRawText && UpdateText != null)
                    {
                        rawText = newRawText;
                        UpdateText(rawText);
                        String translated;
                        try
                        {
                            translated = translator.Translate(rawText, String.Copy(AppKey));
                        }
                        catch (Exception ex)
                        {
                            translated = String.Format("翻译时出错：\r\n{0}", ex.Message);
                        }
                        UpdateText(String.Format("{0}\r\n\r\n\r\n{1}", rawText, translated.Replace("\n", "\r\n")));
                    }
                }
                catch (System.IO.FileNotFoundException ex)
                {
                    UpdateText(String.Format("{0}\r\n\r\n找不到某些 dll 文件, 请检查或重新下载软件。", ex.Message));
                    return;
                }
                catch (Exception ex)
                {
                    UpdateText(String.Format("Worker.Run 中发生异常:\r\n{0}", ex.Message));
                    //return;
                }
            }
        }

        private String getTextFromScreenQrCode()
        {
            Bitmap capture = ScreenCapturer.Capture();
            String newRawText = QrDecoder.MultiDecode(capture);
            if (newRawText != null)
            {
                return newRawText;
            }

            // scale image if no qr code
            double[] factors = { 0.9, 0.8, 0.6 };
            foreach (double factor in factors)
            {
                newRawText = QrDecoder.MultiDecode(capture.Zoom(factor));
                if (newRawText != null)
                {
                    return newRawText;
                }
            }

            return null;
        }
    }
}
