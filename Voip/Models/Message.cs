using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Voip.Models
{
    public class MessageUserItem
    {
        public string Nickname { get; set; }
        public string Avatar { get; set; }
        public string Message { get; set; }
        //public long ContactId { get; set; }
        public long UserId { get; set; }
        public MessageUserItem(string nickname, string avatar, string msg)
        {
            Nickname = nickname;
            Avatar = avatar;
            Message = msg;
        }
        public MessageUserItem()
        {

        }
    }

   
    public class UserMessage
    {
        public string Text { get; set; }
        public string Avatar { get; set; }
        public string CreateTime { get; set; }

        public HorizontalAlignment Horizon { get; set; }

        public int AvatarCol { get; set; }
        public int MessageCol { get; set; }

    }


}
