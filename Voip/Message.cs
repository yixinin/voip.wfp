using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voip
{
	public class UserMessageItem
	{
		public string Nickname { get; set; }
		public string Avatar { get; set; }
		public string Message { get; set; }
		public long ContactId { get; set; }
		public long UserId { get; set; }
		public UserMessageItem(string nickname,string avatar, string msg)
		{
			Nickname = nickname;
			Avatar = avatar;
			Message = msg;
		}
	}
}
