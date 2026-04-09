using System.Collections.Generic;

namespace VibeCoders.Models
{
	public class Trainer : User
    {
        public List<Client> Clients { get; set; } = new List<Client>();
    }
}