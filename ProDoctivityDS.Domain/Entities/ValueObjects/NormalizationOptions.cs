namespace ProDoctivityDS.Domain.Entities.ValueObjects
{

    public class NormalizationOptions
    {
        /// <summary>
        /// Indica si la normalización está habilitada.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Convierte todo el texto a mayúsculas.
        /// </summary>
        public bool ToUpperCase { get; set; } = true;

        /// <summary>
        /// Elimina acentos y caracteres diacríticos.
        /// </summary>
        public bool RemoveAccents { get; set; } = true;

        /// <summary>
        /// Reemplaza los saltos de línea por espacios.
        /// </summary>
        public bool IgnoreLineBreaks { get; set; } = true;

        /// <summary>
        /// Elimina signos de puntuación (todo excepto letras, números y espacios).
        /// </summary>
        public bool RemovePunctuation { get; set; } = true;

        /// <summary>
        /// Colapsa múltiples espacios consecutivos en uno solo y recorta los extremos.
        /// </summary>
        public bool TrimExtraSpaces { get; set; } = true;
    }
}

