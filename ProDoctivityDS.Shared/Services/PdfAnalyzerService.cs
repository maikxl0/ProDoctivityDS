using Microsoft.Extensions.Logging;
using ProDoctivityDS.Application.Dtos.ValueObjects;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Domain.Entities.ValueObjects;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace ProDoctivityDS.Shared.Services
{
    public class PdfAnalyzerService : IPdfAnalyzer
    {
        private readonly ILogger<PdfAnalyzerService> _logger;

        public PdfAnalyzerService(ILogger<PdfAnalyzerService> logger)
        {
            _logger = logger;
        }

        public async Task<string> ExtractFirstPageTextAsync(byte[] pdfBytes, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var stream = new MemoryStream(pdfBytes);
                    using var pdf = PdfDocument.Open(stream);
                    if (pdf.NumberOfPages == 0)
                        return string.Empty;
                    var page = pdf.GetPage(1);
                    return page.Text;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al extraer texto de la primera página del PDF");
                    return string.Empty;
                }
            }, cancellationToken);
        }

        public bool ShouldRemoveFirstPage(string firstPageText, AnalysisRuleSet rules)
        {
            if (string.IsNullOrEmpty(firstPageText))
                return false;

            int limit = rules.SearchCharacterLimit > 0 ? rules.SearchCharacterLimit : int.MaxValue;
            string limitedText = firstPageText.Length > limit ? firstPageText[..limit] : firstPageText;

            string textToEvaluate = rules.Normalization.IsEnabled
                ? NormalizeText(limitedText, rules.Normalization)
                : limitedText;

            bool result1 = CheckSeparador(textToEvaluate, rules.KeywordSeparador, rules.Normalization);
            bool result2 = CheckCodigo(limitedText, textToEvaluate, rules.KeywordCodigo, rules.Normalization);

            return result1 || result2;
        }

        public async Task<AnalysisResultDto> AnalyzePdfAsync(byte[] pdfBytes, AnalysisRuleSet rules, CancellationToken cancellationToken = default)
        {
            var pageResult = await AnalyzePageAsync(pdfBytes, 0, rules, cancellationToken);
            return new AnalysisResultDto
            {
                ShouldRemove = pageResult.ShouldRemove,
                Diagnosis = pageResult.Diagnosis,
                NormalizedText = pageResult.ExtractedTextPreview,
                PageCount = await GetPageCount(pdfBytes)
            };
        }

        public async Task<PageAnalysisResult> AnalyzePageAsync(
            byte[] pdfBytes,
            int pageIndex,
            AnalysisRuleSet rules,
            CancellationToken cancellationToken = default)
        {
            var result = new PageAnalysisResult
            {
                PageNumber = pageIndex + 1,
                ShouldRemove = false
            };

            try
            {
                string pageText = await ExtractPageTextAsync(pdfBytes, pageIndex, cancellationToken);
                result.ExtractedTextPreview = Truncate(pageText, 200);

                int limit = rules.SearchCharacterLimit > 0 ? rules.SearchCharacterLimit : int.MaxValue;
                string originalLimitedText = pageText.Length > limit ? pageText[..limit] : pageText;
                string textToEvaluate = rules.Normalization.IsEnabled
                    ? NormalizeText(originalLimitedText, rules.Normalization)
                    : originalLimitedText;

                bool foundSeparador = CheckSeparador(textToEvaluate, rules.KeywordSeparador, rules.Normalization);
                bool foundCodigo = CheckCodigo(originalLimitedText, textToEvaluate, rules.KeywordCodigo, rules.Normalization);

                result.ShouldRemove = foundSeparador || foundCodigo;

                var diag = new StringBuilder();
                if (foundSeparador)
                    diag.Append($"Separador ('{rules.KeywordSeparador}') encontrado en primeros {limit} caracteres. ");
                if (foundCodigo)
                    diag.Append($"Código ('{rules.KeywordCodigo}') encontrado en primeros {limit} caracteres. ");
                if (!foundSeparador && !foundCodigo)
                    diag.Append($"Ningún criterio coincide en primeros {limit} caracteres. ");

                diag.Append($"Página {result.PageNumber} será {(result.ShouldRemove ? "ELIMINADA" : "CONSERVADA")}.");
                result.Diagnosis = diag.ToString();
                _logger.LogInformation(result.Diagnosis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analizando página {PageIndex}", pageIndex + 1);
                result.Diagnosis = $"Error en análisis: {ex.Message}";
                result.ShouldRemove = false;
            }

            return result;
        }

        // ==================== MÉTODOS PRIVADOS ====================

        private async Task<string> ExtractPageTextAsync(byte[] pdfBytes, int pageIndex, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var stream = new MemoryStream(pdfBytes);
                    using var pdf = PdfDocument.Open(stream);
                    if (pdf.NumberOfPages == 0 || pageIndex >= pdf.NumberOfPages)
                        return string.Empty;
                    var page = pdf.GetPage(pageIndex + 1);
                    return page.Text;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extrayendo texto de página {PageIndex}", pageIndex + 1);
                    return string.Empty;
                }
            }, cancellationToken);
        }

        private async Task<int> GetPageCount(byte[] pdfBytes)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var stream = new MemoryStream(pdfBytes);
                    using var pdf = PdfDocument.Open(stream);
                    return pdf.NumberOfPages;
                }
                catch { return 0; }
            });
        }

        private string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;
            return text[..(maxLength - 3)] + "...";
        }

        /// <summary>
        /// Normaliza el texto según las opciones proporcionadas.
        /// </summary>
        public string NormalizeText(string text, NormalizationOptions options)
        {
            if (string.IsNullOrEmpty(text) || !options.IsEnabled)
                return text;

            var normalized = text;
            if (options.ToUpperCase)
                normalized = normalized.ToUpperInvariant();
            if (options.RemoveAccents)
            {
                var formD = normalized.Normalize(NormalizationForm.FormD);
                var sb = new StringBuilder();
                foreach (var ch in formD)
                {
                    var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                    if (uc != UnicodeCategory.NonSpacingMark)
                        sb.Append(ch);
                }
                normalized = sb.ToString().Normalize(NormalizationForm.FormC);
            }
            if (options.IgnoreLineBreaks)
                normalized = normalized.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
            if (options.RemovePunctuation)
                normalized = Regex.Replace(normalized, @"[^\p{L}\p{N}\s]", " ");
            if (options.TrimExtraSpaces)
                normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

            return normalized;
        }

        /// <summary>
        /// Verifica si el texto contiene alguna frase que indique un separador.
        /// </summary>
        private bool CheckSeparador(string normalizedText, string keyword, NormalizationOptions normalization)
        {
            if (string.IsNullOrEmpty(keyword))
                return false;

            // Normalizar el keyword si la normalización está activada
            string normalizedKeyword = normalization.IsEnabled ? NormalizeText(keyword, normalization) : keyword;

            // Generar variantes a partir del keyword
            var variants = new List<string>
            {
                normalizedKeyword,
                normalizedKeyword.Replace("DE ", ""),
                normalizedKeyword.Replace("SEPARADOR ", ""),
                "SEPARADOR"
            };

            // Añadir frases comunes de separadores (aunque el keyword no las contenga)
            // Esto permite detectar "SEPARADOR DE EXPEDIENTES" aunque la regla sea "SEPARADOR DE DOCUMENTOS"
            var commonPhrases = GetCommonSeparatorPhrases(normalization);
            variants.AddRange(commonPhrases);

            // Eliminar duplicados
            variants = variants.Distinct().ToList();

            foreach (var variant in variants)
            {
                if (!string.IsNullOrEmpty(variant) && normalizedText.Contains(variant, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Verifica si el texto contiene un código de documento (ej. DOC-001, DOC001, etc.)
        /// </summary>
        private bool CheckCodigo(string originalLimitedText, string normalizedText, string keyword, NormalizationOptions normalization)
        {
            if (string.IsNullOrEmpty(keyword))
                return false;

            // 1. Búsqueda de patrones comunes en el texto original (con guiones)
            string[] patterns = { @"DOC-\d+", @"DOC\d+" };
            foreach (var pattern in patterns)
            {
                try
                {
                    if (Regex.IsMatch(originalLimitedText, pattern, RegexOptions.IgnoreCase))
                        return true;
                }
                catch { /* Ignorar regex inválida */ }
            }

            // 2. Búsqueda del keyword normalizado en el texto normalizado (para variantes sin guiones)
            string normalizedKeyword = normalization.IsEnabled ? NormalizeText(keyword, normalization) : keyword;
            if (!string.IsNullOrEmpty(normalizedKeyword) && normalizedText.Contains(normalizedKeyword, StringComparison.Ordinal))
                return true;

            // 3. Búsqueda de variantes simples del keyword (quitando guiones)
            string keywordWithoutDash = keyword.Replace("-", "").Replace(" ", "");
            string normalizedWithoutDash = normalization.IsEnabled ? NormalizeText(keywordWithoutDash, normalization) : keywordWithoutDash;
            if (!string.IsNullOrEmpty(normalizedWithoutDash) && normalizedText.Contains(normalizedWithoutDash, StringComparison.Ordinal))
                return true;

            return false;
        }

        /// <summary>
        /// Devuelve una lista de frases comunes que indican un separador, en español.
        /// Las frases se normalizan según las opciones dadas.
        /// </summary>
        private List<string> GetCommonSeparatorPhrases(NormalizationOptions normalization)
        {
            var rawPhrases = new List<string>
            {
                "SEPARADOR DE DOCUMENTOS",
                "SEPARADOR DE EXPEDIENTES",
                "HOJA SEPARADORA",
                "SEPARADOR"
            };

            if (!normalization.IsEnabled)
                return rawPhrases;

            return rawPhrases.Select(p => NormalizeText(p, normalization)).ToList();
        }
    }
}