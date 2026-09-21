using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ObjetosIngresos.ViewModel
{
    public class RegistrarEquipoViewModel
    {
        public string TipoElemento { get; set; }
        public int? IdMarca { get; set; }
        public string NumeroSerie { get; set; }
        public List<AccesorioItemViewModel> Detalles { get; set; } = new List<AccesorioItemViewModel>();
    }

    public class AccesorioItemViewModel
    {
        public int IdTipoDetalle { get; set; }
        public string Nombre { get; set; }
        public string Especificacion { get; set; }
    }
}