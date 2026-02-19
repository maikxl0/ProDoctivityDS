namespace ProDoctivityDS.Application.Dtos.Request
{
    public class ProcessRequestDto
    {
        /// <summary>
        /// IDs de los documentos a procesar.
        /// </summary>
        public List<string> DocumentIds { get; set; } = new();

        /// <summary>
        /// Sobrescribe la opción "UpdateApi" de la configuración general si se proporciona.
        /// </summary>
        public bool? UpdateApi { get; set; }

        /// <summary>
        /// Sobrescribe la opción "SaveOriginals" de la configuración general si se proporciona.
        /// </summary>
        public bool? SaveOriginals { get; set; }
    }
}