using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Models;
using System.Security.Claims;

namespace ObjetosIngresos.Services
{
    public class UsuarioServices
    {
        private readonly SistemaIngresoContext db;

        public UsuarioServices(SistemaIngresoContext db)
        {
            this.db = db;
        }

        public void Add(Usuario u)
        {
            db.Usuarios.Add(u);
            db.SaveChanges();
        }

        public void Update(Usuario u)
        {
            db.Usuarios.Update(u);
            db.SaveChanges();
        }

        public bool Delete(int id)
        {
            var usuario = db.Usuarios.Find(id);
            if (usuario == null) return false;

            try
            {
                db.Usuarios.Remove(usuario);
                db.SaveChanges();
                return true; 
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Usuario? GetById(int id)
        {
            return db.Usuarios.Find(id);
        }

        public List<Usuario> GetAll()
        {
            return db.Usuarios
                .Include(u => u.IdTipoUsuarioNavigation)
                .Include(u => u.IdSedePrincipalNavigation)
                .ToList();
        }



    }
}
