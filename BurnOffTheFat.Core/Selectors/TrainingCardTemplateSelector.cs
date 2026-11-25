using BurnOffTheFat.Common.Enums;
using BurnOffTheFat.Core.Models;

namespace BurnOffTheFat.Core.Selectors;

/// <summary>
/// Выбор типа шаблона отображения элемента списка тренировок в зависимости от типа тренировки
/// </summary>
public class TrainingCardTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// Шаблон для бега
    /// </summary>
    public DataTemplate? RunningTemplate { get; set; }
    /// <summary>
    /// Шаблон для остальных тренировок TODO: разбить по тренировкам
    /// </summary>
    public DataTemplate? DefaultTemplate { get; set; }
    /// <summary>
    /// Обработка события выбора
    /// </summary>
    /// <param name="item"></param>
    /// <param name="container"></param>
    /// <returns></returns>
    protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
    {
        if (item is TrainingCardModel card)
        {
            return card.TrainingType == SpeedMode.Running
                ? RunningTemplate
                : DefaultTemplate;
        }
        return DefaultTemplate;
    }
}
