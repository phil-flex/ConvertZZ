﻿using ConvertZZ.Moudle;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace ConvertZZ.Pages
{
    /// <summary>
    /// Page_ClipBoard.xaml 的互動邏輯
    /// </summary>
    public partial class Page_ClipBoard : Page, INotifyPropertyChanged
    {
        IntPtr hwnd = IntPtr.Zero;
        public Page_ClipBoard(IntPtr hwnd)
        {
            this.hwnd = hwnd;
            InitializeComponent();
            DataContext = this;
        }

        private string _ClipBoard;
        public string ClipBoard { get => _ClipBoard; set { _ClipBoard = value; OnPropertyChanged("ClipBoard"); } }
        private string _Output;
        public string Output { get => _Output; set { _Output = value; OnPropertyChanged("Output"); } }





        /// <summary>
        /// 編碼轉換 [0]:來源編碼   [1]:輸出編碼
        /// </summary>
        Encoding[] encoding = new Encoding[2] { Encoding.GetEncoding("BIG5"), Encoding.GetEncoding("GBK") };
        /// <summary>
        /// 輸出簡繁轉換：0:一般  1:繁體中文 2:簡體中文
        /// </summary>
        int ToChinese = 0;
        private void Encoding_Selected(object sender, RoutedEventArgs e)
        {
            RadioButton radiobutton = ((RadioButton)sender);
            switch (radiobutton.GroupName)
            {
                case "origin":
                    encoding[0] = Encoding.GetEncoding(((string)radiobutton.Content).Trim());
                    break;
                case "target":
                    encoding[1] = Encoding.GetEncoding(((string)radiobutton.Content).Trim());
                    break;
            }
            Output = ConvertHelper.Convert(ClipBoard, encoding, ToChinese);
        }
        private void Chinese_Click(object sender, RoutedEventArgs e)
        {
            switch (((RadioButton)sender).Uid)
            {
                case "NChinese":
                    ToChinese = 0;
                    break;
                case "TChinese":
                    ToChinese = 1;
                    break;
                case "CChinese":
                    ToChinese = 2;
                    break;
            }
            Output = ConvertHelper.Convert(ClipBoard, encoding, ToChinese);
        }







        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            #region 註冊Hook並監聽剪貼簿            
            hWndSource = HwndSource.FromHwnd(hwnd);
            hWndSource.AddHook(this.WinProc);   // start processing window messages 
            mNextClipBoardViewerHWnd = SetClipboardViewer(hWndSource.Handle);   // set this window as a viewer            
            #endregion
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            ChangeClipboardChain(hWndSource.Handle, mNextClipBoardViewerHWnd);
        }
        private IntPtr WinProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_CHANGECBCHAIN:
                    if (wParam == mNextClipBoardViewerHWnd)
                    {
                        // clipboard viewer chain changed, need to fix it. 
                        mNextClipBoardViewerHWnd = lParam;
                    }
                    else if (mNextClipBoardViewerHWnd != IntPtr.Zero)
                    {
                        // pass the message to the next viewer. 
                        SendMessage(mNextClipBoardViewerHWnd, msg, wParam, lParam);
                    }
                    break;

                case WM_DRAWCLIPBOARD:
                    // clipboard content changed 
                    if (Clipboard.ContainsText())
                    {
                        ClipBoard = ClipBoardHelper.GetClipBoard_UnicodeText();
                        Output = ConvertHelper.Convert(ClipBoard, encoding, ToChinese);
                    }


                    // pass the message to the next viewer. 
                    SendMessage(mNextClipBoardViewerHWnd, msg, wParam, lParam);
                    break;
            }

            return IntPtr.Zero;
        }
        HwndSource hWndSource;
        #region Definitions  
        //Constants for API Calls...  
        private const int WM_DRAWCLIPBOARD = 0x308;
        private const int WM_CHANGECBCHAIN = 0x30D;

        //Handle for next clipboard viewer...  
        private IntPtr mNextClipBoardViewerHWnd;

        //API declarations...  
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static public extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static public extern bool ChangeClipboardChain(IntPtr HWnd, IntPtr HWndNext);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        #endregion

        private void Button_CopyOutput_Click(object sender, RoutedEventArgs e)
        {
            ClipBoardHelper.SetClipBoard_UnicodeText(ConvertHelper.Convert(ClipBoard, encoding, ToChinese));
        }
    }
}
