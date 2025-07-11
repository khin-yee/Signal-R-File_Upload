using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIgnalRTest.Domain.Response
{
    public class Auth0TokenResponse
    {
        public string access_token { get; set; }
        public string id_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
    }
}
