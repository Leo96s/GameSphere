﻿using GameSphere_backend.Models.BackendModels;

namespace GameSphere_backend.Interfaces
{
    public interface IAuthService
    {
        string GenerateToken(User user);

    }
}
