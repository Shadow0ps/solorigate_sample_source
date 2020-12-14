using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using SolarWinds.Orion.Core.Common.ExpressionEvaluator;
using SolarWinds.Orion.Core.Common.ExpressionEvaluator.Functions;
using SolarWinds.Orion.Core.Common.Models.Thresholds;
using SolarWinds.Orion.Core.Common.Thresholds;
using SolarWinds.Orion.Core.Models;
using SolarWinds.Orion.Core.Strings;

namespace SolarWinds.Orion.Core.BusinessLayer.Thresholds
{
	// Token: 0x0200004D RID: 77
	[Export(typeof(IThresholdDataProcessor))]
	public class CoreThresholdProcessor : ExprEvaluationEngine, IThresholdDataProcessor
	{
		// Token: 0x060004CB RID: 1227 RVA: 0x0001E29F File Offset: 0x0001C49F
		public CoreThresholdProcessor()
		{
			base.VariableConvertor = new Func<string, Variable>(this.ConvertVariable);
		}

		// Token: 0x170000B9 RID: 185
		// (get) Token: 0x060004CC RID: 1228 RVA: 0x0001E2DA File Offset: 0x0001C4DA
		protected override IEnumerable<string> AllowedVariables
		{
			get
			{
				return this._variables.Keys;
			}
		}

		// Token: 0x170000BA RID: 186
		// (get) Token: 0x060004CD RID: 1229 RVA: 0x0001E2E7 File Offset: 0x0001C4E7
		protected override IFunctionsDefinition Functions
		{
			get
			{
				return this._functions;
			}
		}

		// Token: 0x060004CE RID: 1230 RVA: 0x0001E2F0 File Offset: 0x0001C4F0
		public ValidationResult IsFormulaValid(string formula, ThresholdLevel level, ThresholdOperatorEnum thresholdOperator)
		{
			if (base.Log.IsDebugEnabled)
			{
				base.Log.DebugFormat("Validating formula: {0} ...", formula);
			}
			ValidationResult validationResult;
			try
			{
				validationResult = this._preProcessor.PreValidateFormula(formula, level, thresholdOperator);
				if (validationResult.IsValid)
				{
					formula = this._preProcessor.PreProcessFormula(formula, level, thresholdOperator);
					this._variables = CoreThresholdProcessor.CreateVariables(CoreThresholdProcessor.CreateDefaultBaselineValues());
					base.TryParse(formula, true);
					validationResult = ValidationResult.CreateValid();
				}
			}
			catch (InvalidInputException ex)
			{
				if (ex.HasError)
				{
					validationResult = new ValidationResult(false, (from er in ex.Errors
					select CoreThresholdProcessor.GetErrorMessage(er)).ToArray<string>());
				}
				else
				{
					validationResult = new ValidationResult(false, ex.Message);
				}
			}
			catch (Exception ex2)
			{
				base.Log.Error(string.Format("Unexpected error when validating formula: {0} ", formula), ex2);
				validationResult = new ValidationResult(false, ex2.Message);
			}
			return validationResult;
		}

		// Token: 0x060004CF RID: 1231 RVA: 0x0001E3F8 File Offset: 0x0001C5F8
		public double ComputeThreshold(string formula, BaselineValues baselineValues, ThresholdLevel level, ThresholdOperatorEnum thresholdOperator)
		{
			if (base.Log.IsVerboseEnabled)
			{
				base.Log.VerboseFormat("Computing formula: {0}, values: [{1}]", new object[]
				{
					formula,
					baselineValues
				});
			}
			if (string.IsNullOrEmpty(formula))
			{
				return 0.0;
			}
			double result;
			try
			{
				formula = this._preProcessor.PreProcessFormula(formula, level, thresholdOperator);
				this._variables = CoreThresholdProcessor.CreateVariables(baselineValues);
				result = base.EvaluateDynamic(formula, this._variables, null);
			}
			catch (InvalidInputException ex)
			{
				string text;
				if (ex.HasError)
				{
					text = string.Join(" ", ex.Errors.Select(new Func<ExprEvalErrorDescription, string>(CoreThresholdProcessor.GetErrorMessage)).ToArray<string>());
				}
				else
				{
					text = ex.Message;
				}
				if (base.Log.IsInfoEnabled)
				{
					base.Log.Info(string.Format("Parsing error: {0} when evaluating formula: {1}, values: {2}", text, formula, baselineValues), ex);
				}
				throw new Exception(text, ex);
			}
			catch (Exception ex2)
			{
				base.Log.Error(string.Format("Unexpected error when evaluating formula: {0}, values: {1}", formula, baselineValues), ex2);
				throw;
			}
			return result;
		}

		// Token: 0x060004D0 RID: 1232 RVA: 0x0001E514 File Offset: 0x0001C714
		public virtual bool IsBaselineValuesValid(BaselineValues baselineValues)
		{
			if (baselineValues == null)
			{
				throw new ArgumentNullException("baselineValues");
			}
			return baselineValues.Mean != null && baselineValues.StdDev != null && baselineValues.Max != null && baselineValues.Min != null && baselineValues.MinDateTime != null && baselineValues.MaxDateTime != null && baselineValues.Timestamp != null;
		}

		// Token: 0x060004D1 RID: 1233 RVA: 0x0001E5A0 File Offset: 0x0001C7A0
		private Variable ConvertVariable(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new Exception("Variable name can't be null or empty.");
			}
			string key = name.ToLowerInvariant();
			if (this._variables.ContainsKey(key))
			{
				return this._variables[key];
			}
			throw new Exception(string.Format(CultureInfo.InvariantCulture, "Can't convert variable {0}.", CoreThresholdProcessor.FormatVariable(name)));
		}

		// Token: 0x060004D2 RID: 1234 RVA: 0x0001E5FC File Offset: 0x0001C7FC
		private static Dictionary<string, Variable> CreateVariables(BaselineValues baselineValues)
		{
			return new Dictionary<string, Variable>
			{
				{
					"mean",
					new Variable
					{
						Type = typeof(double),
						Name = "mean",
						Value = baselineValues.Mean
					}
				},
				{
					"std_dev",
					new Variable
					{
						Type = typeof(double),
						Name = "std_dev",
						Value = baselineValues.StdDev
					}
				},
				{
					"min",
					new Variable
					{
						Type = typeof(double),
						Name = "min",
						Value = baselineValues.Min
					}
				},
				{
					"max",
					new Variable
					{
						Type = typeof(double),
						Name = "max",
						Value = baselineValues.Max
					}
				}
			};
		}

		// Token: 0x060004D3 RID: 1235 RVA: 0x0001E700 File Offset: 0x0001C900
		private static BaselineValues CreateDefaultBaselineValues()
		{
			return new BaselineValues
			{
				Count = 1,
				Max = new double?((double)1),
				Mean = new double?((double)1),
				Min = new double?((double)1),
				StdDev = new double?((double)1)
			};
		}

		// Token: 0x060004D4 RID: 1236 RVA: 0x0001E750 File Offset: 0x0001C950
		private static string GetErrorMessage(ExprEvalErrorDescription err)
		{
			switch (err.Type)
			{
			case ExprEvalErrorType.GeneralError:
				return err.Message;
			case ExprEvalErrorType.LexerError:
			case ExprEvalErrorType.ParsingError:
				if (string.IsNullOrEmpty(err.InvalidText))
				{
					return string.Format(CultureInfo.InvariantCulture, Resources.LIBCODE_ZT0_12, err.CharPosition);
				}
				return string.Format(CultureInfo.InvariantCulture, Resources.LIBCODE_ZT0_13, err.InvalidText, err.CharPosition);
			case ExprEvalErrorType.UnknownVariable:
				return string.Format(CultureInfo.InvariantCulture, Resources.LIBCODE_ZT0_14, CoreThresholdProcessor.FormatVariable(err.InvalidText));
			case ExprEvalErrorType.UnknownFunction:
				return string.Format(CultureInfo.InvariantCulture, Resources.LIBCODE_ZT0_15, err.InvalidText);
			case ExprEvalErrorType.InvalidParametersCount:
				return string.Format(CultureInfo.InvariantCulture, Resources.LIBCODE_ZT0_16, err.InvalidText);
			default:
				return string.Empty;
			}
		}

		// Token: 0x060004D5 RID: 1237 RVA: 0x0001E823 File Offset: 0x0001CA23
		private static string FormatVariable(string name)
		{
			return string.Format(CultureInfo.InvariantCulture, "${0}{1}{2}", "{", name, "}");
		}

		// Token: 0x0400014E RID: 334
		private const string MeanVariableName = "mean";

		// Token: 0x0400014F RID: 335
		private const string StdDevVariableName = "std_dev";

		// Token: 0x04000150 RID: 336
		private const string MinVariableName = "min";

		// Token: 0x04000151 RID: 337
		private const string MaxVariableName = "max";

		// Token: 0x04000152 RID: 338
		private Dictionary<string, Variable> _variables = new Dictionary<string, Variable>();

		// Token: 0x04000153 RID: 339
		private readonly IFunctionsDefinition _functions = new MathFunctionsDefinition();

		// Token: 0x04000154 RID: 340
		private readonly CoreThresholdPreProcessor _preProcessor = new CoreThresholdPreProcessor();
	}
}
