using com.fpnn;
using com.fpnn.proto;

namespace com.fpnn.rum
{
    internal class RUMClientHolder
    {
        private TCPClient client;
        private RegressiveStrategy regressiveStrategy;
        private int connectingFailedCount;
        private long connectedTimestampInMS;
        private long lastConnectingFailedTimestampInMS;

        public RUMClientHolder()
        {
            connectingFailedCount = 0;
            connectedTimestampInMS = 0;
            lastConnectingFailedTimestampInMS = 0;
        }

        public void Init(RUMConfig config)
        {
            regressiveStrategy = new RegressiveStrategy(config.network.regressiveStrategy);

            lock (this)
            {
                connectingFailedCount = 0;
                connectedTimestampInMS = 0;
                lastConnectingFailedTimestampInMS = 0;
            }

            client = TCPClient.Create(config.endpoint, true);
            client.ConnectTimeout = config.network.globalConnectTimeout;
            client.QuestTimeout = config.network.globalQuestTimeout;

            client.SetErrorRecorder(config.errorRecorder);
            client.SetConnectionConnectedDelegate(ConnectionConnectedDelegate);
            client.SetConnectionCloseDelegate(ConnectionCloseDelegate);
        }

        private void ConnectionConnectedDelegate(long connectionId, string endpoint, bool connected)
        {
            lock (this)
            {
                if (connected)
                {
                    connectingFailedCount = 0;
                    connectedTimestampInMS = ClientEngine.GetCurrentMilliseconds();
                    lastConnectingFailedTimestampInMS = 0;
                }
                else
                {
                    connectingFailedCount += 1;
                    connectedTimestampInMS = 0;
                    lastConnectingFailedTimestampInMS = ClientEngine.GetCurrentMilliseconds();
                }
            }
        }

        private void ConnectionCloseDelegate(long connectionId, string endpoint, bool causedByError)
        {
            long now = ClientEngine.GetCurrentMilliseconds();
            lock (this)
            {
                connectedTimestampInMS = 0;

                if (now - connectedTimestampInMS <= regressiveStrategy.connectFailedMaxIntervalMilliseconds)
                {
                    connectingFailedCount += 1;
                    lastConnectingFailedTimestampInMS = now;
                }
                else
                    lastConnectingFailedTimestampInMS = 0;
            }
        }

        public void NetworkUnreachable()
        {
            lock (this)
            {
                connectingFailedCount = 0;
                connectedTimestampInMS = 0;
                lastConnectingFailedTimestampInMS = 0;
            }
        }

        public void EnterFrontground()
        {
            lock (this)
            {
                connectingFailedCount = 0;
                connectedTimestampInMS = 0;
                lastConnectingFailedTimestampInMS = 0;
            }
        }

        private bool CanBeSend()
        {
            lock (this)
            {
                if (connectingFailedCount < regressiveStrategy.startConnectFailedCount)
                    return true;

                long delayMilliseconds = regressiveStrategy.maxIntervalSeconds * 1000;
                if (connectingFailedCount < regressiveStrategy.startConnectFailedCount + regressiveStrategy.linearRegressiveCount)
                {
                    int diff = regressiveStrategy.startConnectFailedCount + regressiveStrategy.linearRegressiveCount - connectingFailedCount;
                    delayMilliseconds /= diff;
                }

                return (lastConnectingFailedTimestampInMS + delayMilliseconds) <= ClientEngine.GetCurrentMilliseconds();
            }
        }

        public TCPClient GetClient()
        {
            return CanBeSend() ? client : null;
        }

        public void Close()
        {
            client.Close();
            client = null;
        }
    }
}