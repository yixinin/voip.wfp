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

        public UserInfo UserInfo;

        private bool isSignUp = false;



        public string Token { get; private set; }

        Utils.HttpClient httpClient;

        public static MainWindow Current { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            httpClient = new Utils.HttpClient(Config.HttpMsgAddr);
            Cellnet.Message.InitMessageIds();

            //读取设置
            //UserInfo = new UserInfo();
            UserInfo = Utils.Cache.GetUserInfo(Utils.Cache.GetDefaultUserName());
            if (UserInfo.Username != null && UserInfo.Username != "")
            {
                uname.Text = UserInfo.Username;
                pwd.Password = UserInfo.Password;
                autoSignCheck.IsChecked = UserInfo.AutoSign;
                remberCheck.IsChecked = UserInfo.Remember;
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
                var deviceCode = Utils.Util.GetDeviceCode();

                var req = new Protocol.SignUpReq
                {
                    Header = new Protocol.ReqHeader(),
                    Username = uname.Text,
                    Password = UserInfo.Password,
                    DeviceCode = deviceCode,
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
                            UserInfo.Avatar = Config.HttpAddr + ack.UserInfo.Avatar;
                        }
                        var users = await GetMessageUsers(ack.Token);
                        var contacts = await GetContacts(ack.Token);
                        OpenCoreWindow(users, contacts);
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
                            UserInfo.Avatar = Config.HttpAddr + ack.UserInfo.Avatar;
                        }

                        var users = await GetMessageUsers(ack.Token);
                        var contacts = await GetContacts(ack.Token);
                        OpenCoreWindow(users, contacts);
                    }
                }
            }
        }

        async public Task<List<Models.ContactItem>> GetContacts(string token)
        {
            var req = new Protocol.GetContactListReq { Header = new Protocol.ReqHeader { Token = token }, };
            var ack = await httpClient.Send<Protocol.GetContactListReq, Protocol.GetContactListAck>(req);
            var contacts = new List<Models.ContactItem>();
            if (ack != null && ack.Header.Code == 200 && ack.Contacts != null)
            {
                foreach (var item in ack.Contacts)
                {
                    contacts.Add(new Models.ContactItem
                    {
                        Avatar = Config.HttpAddr + item.Avatar,
                        ContactId = item.ContactId,
                        Nickname = item.Nickname,
                        UserId = item.UserId,
                    });
                }
            }
            return contacts;
        }

        async public Task<List<Models.MessageUserItem>> GetMessageUsers(string token)
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
                    var users = new List<Models.MessageUserItem>();
                    foreach (var item in ack.Users)
                    {
                        users.Add(new Models.MessageUserItem
                        {
                            Avatar = Config.HttpAddr + item.Avatar,
                            Message = item.Messages?.FirstOrDefault()?.Text,
                            Nickname = item.Nickname,
                            UserId = item.UserId,
                        });
                    }
                    return users;
                }
            }
            return new List<Models.MessageUserItem>();
        }

        public void OpenCoreWindow(List<Models.MessageUserItem> users, List<Models.ContactItem> contacts)
        { 
            
            if (Token == "")
            {
                return;
            }
            //登录成功
            CachePwd();

            //跳转到主界面
            var coreWindow = new ChatWindow();
            foreach (var item in users)
            {
                coreWindow.MessageUsers.Add(item);
            }
            foreach (var item in contacts)
            {
                coreWindow.Contacts.Add(item);
            }
            coreWindow.Token = Token;
            var bmp = new BitmapImage();
            bmp.BeginInit();
            var avatar = UserInfo.Avatar;

            bmp.UriSource = new Uri(avatar);
            bmp.EndInit();
            coreWindow.avatarImg.ImageSource = bmp;
            coreWindow.CellnetClient = new Cellnet.CellnetClient(Config.HOST, Config.TCP_PORT);
            coreWindow.CellnetClient.OnMessage += coreWindow.CellnetClient_OnMessage;
            coreWindow.CellnetClient.Connect();
            if (coreWindow.CellnetClient.IsConnected)
            {
                coreWindow.CellnetClient.Send(new Protocol.EchoReq
                {
                    Header = new Protocol.ReqHeader { Token = Token }
                });
            }
            coreWindow.httpClient = new Utils.HttpClient(Config.HttpMsgAddr);
            coreWindow.UserInfo = UserInfo;

            coreWindow.Show();


            this.Close();

            //}));
        }

        private void CachePwd()
        {
            //登录成功
            //记住密码  
            if (remberCheck.IsChecked.HasValue)
            {
                UserInfo.Remember = remberCheck.IsChecked.Value;
            }
            if (autoSignCheck.IsChecked.HasValue)
            {
                UserInfo.AutoSign = autoSignCheck.IsChecked.Value;
                if (UserInfo.AutoSign)
                {
                    UserInfo.Remember = true;
                }
            }
            if (UserInfo.Remember)
            {
                UserInfo.Password = pwd.Password;
            }
            else
            {
                UserInfo.Password = "";
            }
            UserInfo.UpdateCache(uname.Text);

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
        public bool AutoSign { get; set; }
        public bool Remember { get; set; }
        public string Avatar { get; set; }
        public string DeviceCode { get; set; }

        public string Username { get; set; }

        public long Uid { get; set; }
        public string Nickname { get; set; }


        public UserInfo(string username)
        {
            this.Username = username;
        }

        public UserInfo()
        {

        }
        public void UpdateCache(string username)
        {
            try
            {
                Utils.Cache.SetDefaultUsername(username);
                this.Username = username;
                Utils.Cache.CacheUserInfo(this);

            }
            catch (Exception ex)
            {

            }

        }
    }
}
