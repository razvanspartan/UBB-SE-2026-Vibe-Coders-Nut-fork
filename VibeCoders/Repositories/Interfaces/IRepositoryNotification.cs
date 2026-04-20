namespace VibeCoders.Repositories.Interfaces
{
    using System.Collections.Generic;
    using VibeCoders.Models;
    public interface IRepositoryNotification
    {
        public List<Notification> GetNotifications(int clientId);
        public bool SaveNotification(Notification notification);
    }
}
