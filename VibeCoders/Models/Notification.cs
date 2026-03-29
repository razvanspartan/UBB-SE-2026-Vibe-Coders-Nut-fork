using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VibeCoders.Models
{
    public enum NotificationType
    {
        INCREASE,
        DELOAD,
        PLATEAU
    }

    public class Notification
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        public int TargetTemplateId { get; set; }

        public Notification(string title, string message, NotificationType type, int targetTemplateId)
        {
            Title = title;
            Message = message;
            Type = type;
            TargetTemplateId = targetTemplateId;
        }
    }
}