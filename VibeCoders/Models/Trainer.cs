namespace VibeCoders.Models
{
    using System.Collections.Generic;

    public class Trainer : User
    {
        public List<Client> Clients { get; set; } = new List<Client>();
    }
}