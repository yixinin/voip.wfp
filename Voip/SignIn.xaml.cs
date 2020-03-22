using System;
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

        public Cellnet.CellnetClient cellnetClient;
        public string DefaultAvatar = "http://localhost:8080/static/avatar/default.jpg";
        public UserInfo UserInfo;

        private string TCP_HOST = "10.0.0.218";
        private int TCP_PORT = 8180;
        private bool isSignUp = false;


        List<UserMessageItem> users = new List<UserMessageItem>();

        public string Token { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
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


            cellnetClient = new Cellnet.CellnetClient(TCP_HOST, TCP_PORT);

            cellnetClient.ConnectAsync().Wait();
            if (!cellnetClient.IsConnected)
            {
                MessageBox.Show("无法连接服务器");
                return;
            }
            cellnetClient.OnMessage += CellnetClient_OnMessage;
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
                await cellnetClient.Send(new Protocol.SignUpReq
                {
                    Header = new Protocol.ReqHeader(),
                    Username = uname.Text,
                    Password = UserInfo.Password,
                    DeviceCode = UserInfo.DeviceCode == "" ? Util.GetDeviceCode() : UserInfo.DeviceCode,
                    DeviceType = 2,
                });
            }
            else
            {
                await cellnetClient.Send(new Protocol.SignInReq
                {
                    Username = uname.Text,
                    Password = UserInfo.Password,
                    //DeviceCode = Util.GetDeviceCode(),
                    DeviceType = 2,
                });
            }

            //GotoChat("");
            //this.Close();
        }

        public void CellnetClient_OnMessage(System.Net.Sockets.Socket sender, Cellnet.SocketMessageEventArgs args)
        {
            var t = Cellnet.Message.messages[args.MessaeId];
            if (t.FullName == typeof(Protocol.SignInAck).FullName)
            {
                var msg = Protocol.SignInAck.Parser.ParseFrom(args.Buffer);
                if (msg.Header.Code == 200 && msg.Token != null && msg.Token != "")
                {
                    Token = msg.Token;
                    GetMessageUsers(Token);
                }

            }
            else if (t.FullName == typeof(Protocol.SignUpAck).FullName)
            {
                var msg = Protocol.SignUpAck.Parser.ParseFrom(args.Buffer);
                if (msg.Header.Code == 200 && msg.Token != null && msg.Token != "")
                {
                    UserInfo.DeviceCode = msg.DeviceCode;
                    Token = msg.Token;
                    GetMessageUsers(Token);
                }
            }
            else if (t.FullName == typeof(Protocol.GetMessageUserAck).FullName)
            {
                var msg = Protocol.GetMessageUserAck.Parser.ParseFrom(args.Buffer);
                if (msg.Header.Code == 200 && msg.Users != null)
                {
                    foreach (var user in msg.Users)
                    {
                        users.Add(new UserMessageItem
                        {
                            Avatar = user.Avatar,
                            Message = user.Messages.FirstOrDefault()?.Text,
                            Nickname = user.Nickname,
                            UserId = user.UserId,
                        });
                    }

                    GotoChat();

                }
            }
        }

        public void GetMessageUsers(string token)
        {
            var req = new Protocol.GetMessageUserReq
            {
                Header = new Protocol.ReqHeader { Token = token },
            };
            cellnetClient.Send(req);
        }

        public void GotoChat( )
        {
           

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
            {

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
                var chatPage = new ChatWindow();
                foreach(var item in users)
                {
                    chatPage.MessageUsers.Add(item);
                }
                chatPage.Token = Token;
                var bmp = new BitmapImage();
                bmp.BeginInit();
                var avatar = UserInfo.Avatar;
                if (avatar == null || avatar == "")
                {
                    avatar = DefaultAvatar;
                }
                bmp.UriSource = new Uri(avatar);
                bmp.EndInit();
                chatPage.avatarImg.ImageSource = bmp;
                chatPage.CellnetClient = cellnetClient;
                chatPage.CellnetClient.OnMessage += chatPage.CellnetClient_OnMessage;

                chatPage.Show();

                cellnetClient.OnMessage -= CellnetClient_OnMessage;
                this.cellnetClient = null;
                this.Close();
            }));
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


        public UserInfo(string username = "")
        {
            if (username == "")
            {
                //读取默认用户 
                Username = Util.GetDefaultUserName();
                username = Username;
            }
            //读取设置
            //this.Username = Util.GetSettingString(Util.Username,username);
            this.Password = Util.GetSettingString(Util.Password, username);
            this.AutoSign = Util.GetSettingString(Util.AutoSignIn, username);
            this.Remember = Util.GetSettingString(Util.Remember, username);
            this.Avatar = Util.GetSettingString(Util.Avatar, username);
            this.DeviceCode = Util.GetSettingString(Util.DeviceCode, username);
        }

        public void UpdateCache(string username)
        {
            Util.SetDefaultUsername(username);
            if (Remember == "1")
            {
                Util.UpdateSettingString(Util.Password, username, Password);
            }
            else
            {
                Util.UpdateSettingString(Util.Password, username, "");
            }
            Util.UpdateSettingString(Util.AutoSignIn, username, AutoSign);
            Util.UpdateSettingString(Util.Remember, username, Remember);
            Util.UpdateSettingString(Util.Avatar, username, Avatar);
            Util.UpdateSettingString(Util.DeviceCode, username, DeviceCode);
        }
    }
}
