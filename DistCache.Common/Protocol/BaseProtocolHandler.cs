using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using DistCache.Common.NetworkManagement;
using DistCache.Common.Utilities;
using DistCache.Common.Protocol.Messages;

namespace DistCache.Common.Protocol
{
    public abstract class BaseProtocolHandler : SocketHandler
    {
        public class TaskCompletionDisposableSource : TaskCompletionSource<object>, IDisposable
        {
            private BaseProtocolHandler _protocolHandler;
            public Guid Id { get; private set; }

            internal TaskCompletionDisposableSource(BaseProtocolHandler protocolHandler, Guid? id=null) : base()
            {
                Id = id ?? Guid.NewGuid();
                this._protocolHandler = protocolHandler;
            }

            public void Dispose()
            {
                this._protocolHandler._tasksAwaitingResponse.TryRemove(Id, out TaskCompletionDisposableSource ingoreMe);
            }
        }

        protected ConcurrentDictionary<Guid, TaskCompletionDisposableSource> _tasksAwaitingResponse = new ConcurrentDictionary<Guid, TaskCompletionDisposableSource>();

        protected BaseProtocolHandler(SocketHandler socket) : base(socket)
        {
        }

        public TaskCompletionDisposableSource CreateTaskCompletionDisposableSource(Guid? id = null)
        {
            TaskCompletionDisposableSource result = new TaskCompletionDisposableSource(this, id);
            this._tasksAwaitingResponse.TryAdd(result.Id, result);
            return result;
        }

        protected override sealed bool HandleMessages(byte[] message)
        {
            var deserd = BsonUtilities.Deserialise<BaseMessage>(message);

            switch (deserd.MessageType)
            {
                case MessageTypeEnum.Reponse:
                    {
                        HandleResponse(deserd as BaseResponse);
                        break;
                    }
                case MessageTypeEnum.Request:
                    {
                        HandleRequest(deserd as BaseRequest);
                        break;
                    }
                default:
                    {
                        throw new Exception("unknown stuff");
                    }
            }
            return true;
        }

        protected abstract void HandleResponse(BaseResponse baseResponse);
        protected abstract void HandleRequest(BaseRequest baseRequest);
    }
}
