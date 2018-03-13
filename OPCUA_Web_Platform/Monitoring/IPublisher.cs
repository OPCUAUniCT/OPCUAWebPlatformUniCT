namespace WebPlatform.Monitoring
{
    /// <summary>
    /// Every Publisher for the monitoring shall implement this interface
    /// </summary>
    public interface IPublisher
    {
        void Publish(string topic, string message);
    }
}