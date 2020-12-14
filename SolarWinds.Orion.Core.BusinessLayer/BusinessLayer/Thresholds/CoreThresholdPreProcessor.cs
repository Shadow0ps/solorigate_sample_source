using System;
using SolarWinds.Orion.Core.Common.Models.Thresholds;
using SolarWinds.Orion.Core.Models;
using SolarWinds.Orion.Core.Strings;

namespace SolarWinds.Orion.Core.BusinessLayer.Thresholds
{
	// Token: 0x0200004C RID: 76
	internal class CoreThresholdPreProcessor
	{
		// Token: 0x060004C4 RID: 1220 RVA: 0x0001E0A4 File Offset: 0x0001C2A4
		public string PreProcessFormula(string formula, ThresholdLevel level, ThresholdOperatorEnum thresholdOperator)
		{
			if (string.IsNullOrEmpty(formula))
			{
				return formula;
			}
			if (!this.IsUseBaselineMacro(formula))
			{
				return formula;
			}
			if (this.IsMacro(BusinessLayerSettings.Instance.ThresholdsUseBaselineCriticalCalculationMacro, formula))
			{
				level = ThresholdLevel.Critical;
			}
			if (this.IsMacro(BusinessLayerSettings.Instance.ThresholdsUseBaselineWarningCalculationMacro, formula))
			{
				level = ThresholdLevel.Warning;
			}
			if (CoreThresholdPreProcessor.IsGreaterOperator(thresholdOperator))
			{
				if (level != ThresholdLevel.Critical)
				{
					return BusinessLayerSettings.Instance.ThresholdsDefaultWarningFormulaForGreater;
				}
				return BusinessLayerSettings.Instance.ThresholdsDefaultCriticalFormulaForGreater;
			}
			else
			{
				if (level != ThresholdLevel.Critical)
				{
					return BusinessLayerSettings.Instance.ThresholdsDefaultWarningFormulaForLess;
				}
				return BusinessLayerSettings.Instance.ThresholdsDefaultCriticalFormulaForLess;
			}
		}

		// Token: 0x060004C5 RID: 1221 RVA: 0x0001E130 File Offset: 0x0001C330
		public ValidationResult PreValidateFormula(string formula, ThresholdLevel level, ThresholdOperatorEnum thresholdOperator)
		{
			if (string.IsNullOrEmpty(formula))
			{
				return new ValidationResult(false, string.Format(Resources.LIBCODE_PC0_01, Array.Empty<object>()));
			}
			if (this.IsUseBaselineMacro(formula))
			{
				if (thresholdOperator == ThresholdOperatorEnum.Equal || thresholdOperator == ThresholdOperatorEnum.NotEqual)
				{
					return new ValidationResult(false, string.Format(Resources.LIBCODE_ZT0_11, formula));
				}
			}
			else
			{
				if (this.ContainsMacro(BusinessLayerSettings.Instance.ThresholdsUseBaselineCalculationMacro, formula))
				{
					return new ValidationResult(false, string.Format(Resources.LIBCODE_ZT0_17, BusinessLayerSettings.Instance.ThresholdsUseBaselineCalculationMacro));
				}
				if (this.ContainsMacro(BusinessLayerSettings.Instance.ThresholdsUseBaselineWarningCalculationMacro, formula))
				{
					return new ValidationResult(false, string.Format(Resources.LIBCODE_ZT0_17, BusinessLayerSettings.Instance.ThresholdsUseBaselineWarningCalculationMacro));
				}
				if (this.ContainsMacro(BusinessLayerSettings.Instance.ThresholdsUseBaselineCriticalCalculationMacro, formula))
				{
					return new ValidationResult(false, string.Format(Resources.LIBCODE_ZT0_17, BusinessLayerSettings.Instance.ThresholdsUseBaselineCriticalCalculationMacro));
				}
			}
			return ValidationResult.CreateValid();
		}

		// Token: 0x060004C6 RID: 1222 RVA: 0x0001E210 File Offset: 0x0001C410
		private static bool IsGreaterOperator(ThresholdOperatorEnum thresholdOperator)
		{
			return thresholdOperator == ThresholdOperatorEnum.Greater || thresholdOperator == ThresholdOperatorEnum.GreaterOrEqual;
		}

		// Token: 0x060004C7 RID: 1223 RVA: 0x0001E21C File Offset: 0x0001C41C
		private bool IsUseBaselineMacro(string formula)
		{
			string text = formula.Trim();
			return text.Equals(BusinessLayerSettings.Instance.ThresholdsUseBaselineCalculationMacro, StringComparison.InvariantCultureIgnoreCase) || text.Equals(BusinessLayerSettings.Instance.ThresholdsUseBaselineWarningCalculationMacro, StringComparison.InvariantCultureIgnoreCase) || text.Equals(BusinessLayerSettings.Instance.ThresholdsUseBaselineCriticalCalculationMacro, StringComparison.InvariantCultureIgnoreCase);
		}

		// Token: 0x060004C8 RID: 1224 RVA: 0x0001E269 File Offset: 0x0001C469
		private bool IsMacro(string macro, string formula)
		{
			return formula.Trim().ToUpper().Equals(macro, StringComparison.InvariantCultureIgnoreCase);
		}

		// Token: 0x060004C9 RID: 1225 RVA: 0x0001E27D File Offset: 0x0001C47D
		private bool ContainsMacro(string macro, string formula)
		{
			return !string.IsNullOrEmpty(macro) && !string.IsNullOrEmpty(formula) && formula.IndexOf(macro, StringComparison.InvariantCultureIgnoreCase) >= 0;
		}
	}
}
