using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;

namespace Bacc_front
{
    public class MyServerConfig : IServerConfig
    {
        public IServerConfig config;
        //public MyServerConfig instance;
        public MyServerConfig(IAppServer he)
        {
            config = he.Config;
            //instance =(MyServerConfig)MemberwiseClone();
        }
        int IServerConfig.Port => config.Port;

        //int IServerConfig.MaxRequestLength => 204800;
        //int IServerConfig.MaxRequestLength => 2048;
        int IServerConfig.MaxRequestLength => config.MaxRequestLength;

        //int IServerConfig.ReceiveBufferSize => 20480;
        int IServerConfig.ReceiveBufferSize => config.ReceiveBufferSize;

        //int IServerConfig.SendBufferSize => 204800;
        int IServerConfig.SendBufferSize => config.SendBufferSize;

        string IServerConfig.ServerTypeName => config.ServerTypeName;

        string IServerConfig.ServerType => config.ServerType;

        string IServerConfig.ReceiveFilterFactory => config.ReceiveFilterFactory;

        string IServerConfig.Ip => config.Ip;


        NameValueCollection IServerConfig.Options => config.Options;

        NameValueCollection IServerConfig.OptionElements => config.OptionElements;

        bool IServerConfig.Disabled => config.Disabled;

        string IServerConfig.Name => config.Name;

        SocketMode IServerConfig.Mode => config.Mode;

        int IServerConfig.SendTimeOut => config.SendTimeOut;

        int IServerConfig.MaxConnectionNumber => config.MaxConnectionNumber;

       

        bool IServerConfig.SyncSend => config.SyncSend;

        bool IServerConfig.LogCommand => config.LogCommand;

        bool IServerConfig.ClearIdleSession => config.ClearIdleSession;

        int IServerConfig.ClearIdleSessionInterval => config.ClearIdleSessionInterval;

        int IServerConfig.IdleSessionTimeOut => config.IdleSessionTimeOut;

        ICertificateConfig IServerConfig.Certificate => config.Certificate;

        string IServerConfig.Security => config.Security;

       

        bool IServerConfig.DisableSessionSnapshot => config.DisableSessionSnapshot;

        int IServerConfig.SessionSnapshotInterval => config.SessionSnapshotInterval;

        string IServerConfig.ConnectionFilter => config.ConnectionFilter;

        string IServerConfig.CommandLoader => config.CommandLoader;

        int IServerConfig.KeepAliveTime => config.KeepAliveTime;

        int IServerConfig.KeepAliveInterval => config.KeepAliveInterval;

        int IServerConfig.ListenBacklog => config.ListenBacklog;

        int IServerConfig.StartupOrder => config.StartupOrder;

        IEnumerable<IListenerConfig> IServerConfig.Listeners => config.Listeners;

        string IServerConfig.LogFactory => config.LogFactory;

        int IServerConfig.SendingQueueSize => config.SendingQueueSize;

        bool IServerConfig.LogBasicSessionActivity => config.LogBasicSessionActivity;

        bool IServerConfig.LogAllSocketException => config.LogAllSocketException;

        string IServerConfig.TextEncoding => config.TextEncoding;

        IEnumerable<ICommandAssemblyConfig> IServerConfig.CommandAssemblies => config.CommandAssemblies;

        TConfig IServerConfig.GetChildConfig<TConfig>(string childConfigName)
        {
            //return config.GetChildConfig<TConfig>(childConfigName);
            throw new NotImplementedException();
        }
    }
}
