﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Voip
{
    /// <summary>
    /// SignIn.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        //public Cellnet.CellnetClient cellnetClient;
        public string DefaultAvatar = "http://localhost:8080/static/avatar/default.jpg";
        public UserInfo UserInfo;

        private string TCP_HOST = "127.0.0.1";
        private int TCP_PORT = 8180;
        private bool isSignUp = false;

        public string HttpURL = "http://localhost:8080/livechat/msg";

        //List<Models.MessageUserItem> users = new List<Models.MessageUserItem>();

        public string Token { get; private set; }

        Utils.HttpClient httpClient;

        public static MainWindow Current { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            httpClient = new Utils.HttpClient(HttpURL);
            Cellnet.Message.InitMessageIds();

            //读取设置
            UserInfo = new UserInfo();
            if (UserInfo.Username != null && UserInfo.Username != "")
            {
                uname.Text = UserInfo.Username;
                pwd.Password = UserInfo.Password;
                autoSignCheck.IsChecked = UserInfo.AutoSign == "1";
                remberCheck.IsChecked = UserInfo.Remember == "1";
            }

            Current = this;
        }

        private void signUpBtn_Click(object sender, RoutedEventArgs e)
        {
            isSignUp = !isSignUp;
            if (isSignUp)
            {
                signUpBtn.Content = "直接登录";
                signInBtn.Content = "注册";
            }
            else
            {
                signUpBtn.Content = "注册账号";
                signInBtn.Content = "登录";
            }
        }

        async private void signInBtn_Click(object sender, RoutedEventArgs e)
        {
            UserInfo.Password = pwd.Password;
            if (isSignUp)
            {
                var req = new Protocol.SignUpReq
                {
                    Header = new Protocol.ReqHeader(),
                    Username = uname.Text,
                    Password = UserInfo.Password,
                    DeviceCode = UserInfo.DeviceCode == "" ? Utils.Util.GetDeviceCode() : UserInfo.DeviceCode,
                    DeviceType = 2,
                };
                var ack = await httpClient.Send<Protocol.SignUpReq, Protocol.SignUpAck>(req);
                if (ack != null && ack.Header != null)
                {
                    if (ack.Header.Code == 200 && ack.Token != null && ack.Token != "")
                    {
                        if (ack.UserInfo != null)
                        {
                            UserInfo.Uid = ack.UserInfo.Uid;
                            UserInfo.Nickname = ack.UserInfo.Nickname;
                            UserInfo.Avatar = ack.UserInfo.Avatar;
                        }
                        await GetMessageUsers(ack.Token);
                    }
                }
            }
            else
            {
                var req = new Protocol.SignInReq
                {
                    Username = uname.Text,
                    Password = UserInfo.Password,
                    //DeviceCode = Utils.Cache.GetDeviceCode(),
                    DeviceType = 2,
                };
                var ack = await httpClient.Send<Protocol.SignInReq, Protocol.SignInAck>(req);
                if (ack != null && ack.Header != null)
                {
                    if (ack.Header.Code == 200 && ack.Token != null && ack.Token != "")
                    {
                        if (ack.UserInfo != null)
                        {
                            UserInfo.Uid = ack.UserInfo.Uid;
                            UserInfo.Nickname = ack.UserInfo.Nickname;
                            UserInfo.Avatar = ack.UserInfo.Avatar;
                        }

                        await GetMessageUsers(ack.Token);
                    }
                }
            }
        }

        async public Task GetMessageUsers(string token)
        {
            Token = token;
            var req = new Protocol.GetMessageUserReq
            {
                Header = new Protocol.ReqHeader { Token = token },
            };
            //cellnetClient.Send(req);
            var ack = await httpClient.Send<Protocol.GetMessageUserReq, Protocol.GetMessageUserAck>(req);
            if (ack != null && ack.Header != null)
            {
                if (ack.Header.Code == 200 && ack.Users != null)
                {

                    //Utils.Database.connection.
                    //缓存数据


                    var users = new List<Models.MessageUserItem>();
                    foreach (var item in ack.Users)
                    {
                        users.Add(new Models.MessageUserItem
                        {
                            Avatar = item.Avatar,
                            Message = item.Messages?.FirstOrDefault()?.Text,
                            Nickname = item.Nickname,
                            UserId = item.UserId,
                        });
                    }
                }
            }
        }

        public void OpenCoreWindow(List<Models.MessageUserItem> users)
        {


            //Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
            //{

            //登录成功
            if (Token != "")
            {
                //登录成功
                //记住密码  
                if (remberCheck.IsChecked.HasValue)
                {
                    UserInfo.Remember = remberCheck.IsChecked.Value ? "1" : "0";
                }
                if (autoSignCheck.IsChecked.HasValue)
                {
                    UserInfo.Remember = "1";
                    UserInfo.AutoSign = autoSignCheck.IsChecked.Value ? "1" : "0";
                }
                UserInfo.UpdateCache(uname.Text);

            }

            //跳转到主界面
            var coreWindow = new ChatWindow();
            foreach (var item in users)
            {
                coreWindow.MessageUsers.Add(item);
            }
            coreWindow.Token = Token;
            var bmp = new BitmapImage();
            bmp.BeginInit();
            var avatar = UserInfo.Avatar;
            if (avatar == null || avatar == "")
            {
                avatar = DefaultAvatar;
            }
            bmp.UriSource = new Uri(avatar);
            bmp.EndInit();
            coreWindow.avatarImg.ImageSource = bmp;
            coreWindow.CellnetClient = new Cellnet.CellnetClient(TCP_HOST, TCP_PORT);
            coreWindow.CellnetClient.OnMessage += coreWindow.CellnetClient_OnMessage;
            coreWindow.httpClient = new Utils.HttpClient(HttpURL);
            coreWindow.UserInfo = UserInfo;
            coreWindow.Show();


            this.Close();

            //}));
        }

      
        private void autoSignCheck_Checked(object sender, RoutedEventArgs e)
        {
            remberCheck.IsChecked = true;
        }
    }



    public class UserInfo
    {
        //public string Username { get; set; }
        public string Password { get; set; }
        public string AutoSign { get; set; }
        public string Remember { get; set; }
        public string Avatar { get; set; }
        public string DeviceCode { get; set; }

        public string Username { get; set; }

        public long Uid { get; set; }
        public string Nickname { get; set; }


        public UserInfo(string username)
        {
            if (username == "")
            {
                //读取默认用户 
                Username = Utils.Cache.GetDefaultUserName();
                username = Username;
            }
            //读取设置
            //this.Username = Utils.Cache.GetSettingString(Utils.Cache.Username,username);
            this.Password = Utils.Cache.GetSettingString(Utils.Cache.Password, username);
            this.AutoSign = Utils.Cache.GetSettingString(Utils.Cache.AutoSignIn, username);
            this.Remember = Utils.Cache.GetSettingString(Utils.Cache.Remember, username);
            this.Avatar = Utils.Cache.GetSettingString(Utils.Cache.Avatar, username);
            this.DeviceCode = Utils.Cache.GetSettingString(Utils.Cache.DeviceCode, username);
        }

        public UserInfo()
        {

        }
        public void UpdateCache(string username)
        {
            Utils.Cache.SetDefaultUsername(username);
            if (Remember == "1")
            {
                Utils.Cache.UpdateSettingString(Utils.Cache.Password, username, Password);
            }
            else
            {
                Utils.Cache.UpdateSettingString(Utils.Cache.Password, username, "");
            }
            Utils.Cache.UpdateSettingString(Utils.Cache.AutoSignIn, username, AutoSign);
            Utils.Cache.UpdateSettingString(Utils.Cache.Remember, username, Remember);
            Utils.Cache.UpdateSettingString(Utils.Cache.Avatar, username, Avatar);
            Utils.Cache.UpdateSettingString(Utils.Cache.DeviceCode, username, DeviceCode);
        }
    }
}
