using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voip.Models
{
    public class ContactItem
    {
        public string Nickname { get; set; }
        public string Avatar { get; set; }
        public long ContactId { get; set; }
        public long UserId { get; set; }
    }

}
