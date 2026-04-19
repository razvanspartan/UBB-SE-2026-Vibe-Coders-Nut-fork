// <copyright file="Trainer.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace VibeCoders.Models
{
    using System.Collections.Generic;

    public class Trainer : User
    {
        public List<Client> Clients { get; set; } = new List<Client>();
    }
}