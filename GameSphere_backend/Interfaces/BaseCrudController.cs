using GameSphere_backend.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace GameSphere_backend.Interfaces
{
    public abstract class BaseCrudController<T> : ResponseController
    {
        public BaseCrudController(IConfiguration configuration) : base(configuration)
        {
        }
        public abstract Task<IActionResult> GetEntityById(int id);
        public abstract Task<IActionResult> CreateEntity(T entity);
        public abstract Task<IActionResult> UpdateEntity(int id, T entity);
        public abstract Task<IActionResult> DeleteEntity(int id);


    }
}
