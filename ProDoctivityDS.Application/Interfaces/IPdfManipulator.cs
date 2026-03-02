namespace ProDoctivityDS.Application.Interfaces
{
    public interface IPdfManipulator
    {
        /// <summary>
        /// Elimina la primera página del PDF. Si solo tiene una página, retorna el original.
        /// </summary>
        Task<byte[]> RemovePagesAsync(
        byte[] pdfBytes,
        IEnumerable<int> pageIndices,
        CancellationToken cancellationToken = default);
        Task<byte[]> RemoveFirstPageAsync(byte[] pdfBytes, CancellationToken cancellationToken = default);

        // Método auxiliar para obtener número de páginas sin cargar todo el documento (útil para progreso)
        Task<int> GetPageCountAsync(byte[] pdfBytes, CancellationToken cancellationToken = default);
    }
}
