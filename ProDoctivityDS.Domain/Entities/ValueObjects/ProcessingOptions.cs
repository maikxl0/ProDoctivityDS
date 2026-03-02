namespace ProDoctivityDS.Domain.Entities.ValueObjects
{
    public class ProcessingOptions
    {
        public bool RemoveFirstPage { get; set; } = true;
        public bool OnlyIfCriteriaMet { get; set; } = true;
        public bool UpdateApi { get; set; } = false;
        public bool SaveOriginalFiles { get; set; } = false;
        public bool AutoRemoveAllSeparators { get; set; } = false;
        public bool CreateBackup { get; set; } = false; // reservado
        public bool RemovePagesEnabled { get; set; }               // Activar eliminación de páginas
        public string PagesToRemove { get; set; } = "1";          // Ej: "1,3-5,7"
        public string RemoveMode { get; set; } = "specific";       // "specific" o "range"
        public int PageRangeStart { get; set; } = 1;
        public int PageRangeEnd { get; set; } = 1;
        public bool AnalyzeAllPages { get; set; }                  // Analizar todas las páginas antes de eliminar
        public bool ShowExtractedText { get; set; }                // Mostrar texto extraído en logs
    }
}

