using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistCache.Common.Protocol.Messages
{
    public enum MessageTypeEnum
    {
        ClientAuthRequest,
        ServerAuthRequest,
        AuthRequestOk,
        AuthRequestError,
        KeepAlive,
        Request,
        Reponse,
    }
}
