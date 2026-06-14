namespace Rmms.Domain.Enums;

/// <summary>Preset form templates (M10 + 04-data-model.md forms.form_type). Stored as varchar.</summary>
public enum FormType
{
    StockReport,
    MarketReport,
    PhotoReport,
    PcChecklist,
    FreeReport,
    Survey,
    KnowledgeTest,
    Training,
    VisitReport,
}
