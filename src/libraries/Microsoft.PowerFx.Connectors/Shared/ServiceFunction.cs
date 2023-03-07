﻿// <autogenerated>
// Use autogenerated to suppress styelcop warnings since this is shared from another repo.

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
#if canvas
using Microsoft.AppMagic.Authoring.Publish;
using Microsoft.AppMagic.DocumentServer.Common;
#endif
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Publish;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Contracts = Microsoft.PowerFx.Core.Utils.Contracts;
using Microsoft.PowerFx;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;
using System.Threading;

namespace Microsoft.AppMagic.Authoring.Texl.Builtins
{
    [System.Diagnostics.DebuggerDisplay("ServiceFunction: {LocaleSpecificName}")]
    // [RequiresErrorContext]
    internal sealed class ServiceFunction : BuiltinFunction
#if !canvas
        , IAsyncTexlFunction
#endif
    {
        private readonly List<string[]> _signatures;
        private readonly string[] _orderedRequiredParams;
        private readonly Dictionary<string, TypedName> _optionalParamInfo;
        private readonly Dictionary<string, string> _parameterDescriptionMap;
        private readonly bool _isBehaviorOnly;
        private readonly bool _isAutoRefreshable;
        private readonly bool _isDynamic;
        private readonly bool _isCacheEnabled;
        private readonly int _cacheTimeoutMs;
        private readonly bool _isHidden;
        private readonly Dictionary<TypedName, List<string>> _parameterOptions;
        private readonly Dictionary<string, Tuple<string, DType>> _parameterDefaultValues;
        private readonly WeakReference<IService> _parentService;
        private readonly string _actionName;

        public IEnumerable<TypedName> OptionalParams => _optionalParamInfo.Values;
        public Dictionary<string, TypedName> OptionalParamInfo => _optionalParamInfo;
        public override Capabilities Capabilities => Capabilities.OutboundInternetAccess | Capabilities.EnterpriseAuthentication | Capabilities.PrivateNetworkAccess;
        public override bool IsHidden => _isHidden;
        public override bool IsSelfContained => !_isBehaviorOnly;

        internal string ServiceFunctionName => Name;
        internal string ServiceFunctionNamespace => Namespace.Name;

        public ServiceFunction(IService parentService, DPath theNamespace, string name, string localeSpecificName, string description,
            DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, bool isBehaviorOnly, bool isAutoRefreshable, bool isDynamic, bool isCacheEnabled, int cacheTimetoutMs, bool isHidden,
            Dictionary<TypedName, List<string>> parameterOptions, ServiceFunctionParameterTemplate[] optionalParamInfo, ServiceFunctionParameterTemplate[] requiredParamInfo,
            Dictionary<string, Tuple<string, DType>> parameterDefaultValues, string actionName = "", params DType[] paramTypes)
            : base(theNamespace, name, localeSpecificName, (l) => description, FunctionCategories.REST, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        {
            Contracts.AssertValueOrNull(parentService);
            Contracts.AssertValueOrNull(localeSpecificName);
            Contracts.AssertValue(description);
            Contracts.AssertValue(parameterOptions);
            Contracts.AssertValue(optionalParamInfo);
            Contracts.AssertValue(requiredParamInfo);
            Contracts.AssertValue(parameterDefaultValues);
            Contracts.AssertValue(paramTypes);

            // These asserts verify that the parameter containers have the correct length.
            Contracts.Assert(paramTypes.Length == arityMax);
            Contracts.Assert(optionalParamInfo.Length != 0 || (
                (arityMin == arityMax) &&
                (paramTypes.Length == requiredParamInfo.Length)));
            Contracts.Assert(optionalParamInfo.Length == 0 || (
                (arityMax == arityMin + 1) &&
                (paramTypes.Length == requiredParamInfo.Length + 1)));
            Contracts.Assert(arityMin <= arityMax && arityMax <= arityMin + 1,
                "We only support up to one additional options argument");

            if (parentService != null)
                _parentService = new WeakReference<IService>(parentService, trackResurrection: false);

            _optionalParamInfo = new Dictionary<string, TypedName>(optionalParamInfo.Length);
            _parameterDescriptionMap = new Dictionary<string, string>(requiredParamInfo.Length);
            foreach (var optionalParam in optionalParamInfo)
            {
                _optionalParamInfo.Add(optionalParam.TypedName.Name, optionalParam.TypedName);
                _parameterDescriptionMap.Add(optionalParam.TypedName.Name.Value, optionalParam.Description);
            }

            foreach (var requiredParam in requiredParamInfo)
                _parameterDescriptionMap.Add(requiredParam.TypedName.Name.Value, requiredParam.Description);

            _signatures = new List<string[]>();
            _parameterOptions = parameterOptions;
            _isBehaviorOnly = isBehaviorOnly;
            _isAutoRefreshable = isAutoRefreshable;
            _isDynamic = isDynamic;
            _isCacheEnabled = isCacheEnabled;
            _cacheTimeoutMs = cacheTimetoutMs;
            _isHidden = isHidden;
            _orderedRequiredParams = requiredParamInfo.Select(p => p.TypedName.Name.Value).ToArray();
            _signatures.Add(_orderedRequiredParams);
            _parameterDefaultValues = parameterDefaultValues;
            _actionName = actionName;

            if (arityMax > arityMin)
            {
                Contracts.Assert(arityMax == arityMin + 1, "We currently only expect one extra param, holding the object with the optional arguments specified by name.");

                string[] optionalSignature = new string[arityMax];
                _orderedRequiredParams.CopyTo(optionalSignature, 0);

                var optionFormat = new StringBuilder(TexlLexer.PunctuatorCurlyOpen);
                string sep = "";
                string listSep = TexlLexer.GetLocalizedInstance(CultureInfo.CurrentCulture).LocalizedPunctuatorListSeparator + " ";
                foreach (var option in optionalParamInfo)
                {
                    optionFormat.Append(sep);
                    optionFormat.Append(TexlLexer.EscapeName(option.TypedName.Name.ToString()));
                    optionFormat.Append(TexlLexer.PunctuatorColon);
                    optionFormat.Append(option.TypedName.Type.GetKindString());
                    sep = listSep;
                }
                optionFormat.Append(TexlLexer.PunctuatorCurlyClose);

                optionalSignature[arityMax - 1] = optionFormat.ToString();
                _signatures.Add(optionalSignature);
            }
        }

        public string ActionName { get { return _actionName; } }

        // Service functions are asyncronous
        public override bool IsAsync { get { return true; } }

        // Multiple invocations with the same args may result in different return values.
        public override bool IsStateless { get { return false; } }

        // This function may or may not be behavior-only.
        public override bool IsBehaviorOnly { get { return _isBehaviorOnly; } }

        public override bool IsAutoRefreshable { get { return _isAutoRefreshable; } }

#if canvas
        public override bool IsDynamic { get { return _isDynamic && FeatureGates.DocumentPreviewFlags.DynamicSchema; } }
#endif

        public bool IsCacheEnabled { get { return _isCacheEnabled; } }

        public int CacheTimeoutMs { get { return _cacheTimeoutMs; } }

        // Service functions currently require all columns to be filled.
        public override bool RequireAllParamColumns { get { return true; } }

        // Service functions do not have hardwired help links.
        // If some service function happens to have a help link (URL), it would have to be returned by this override.
        public override string HelpLink { get { return string.Empty; } }

        public IService ParentService
        {
            get
            {
                IService service;
                if (_parentService != null && _parentService.TryGetTarget(out service))
                    return service;

                return null;
            }
        }

        // Helper class so that we can return StringGetters from GetSignatures
        private class CaptureString
        {
            private string value;

            public CaptureString(string what)
            {
                value = what;
            }

            public string GetValue(string locale)
            {
                return value;
            }
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            foreach (string[] signature in _signatures)
            {
                var getters = new TexlStrings.StringGetter[signature.Length];
                for (int i = 0; i < signature.Length; i++)
                {
                    var captureValue = new CaptureString(signature[i]);
                    getters[i] = captureValue.GetValue;
                }

                yield return getters;
            }
        }

#if canvas
        public DynamicTypeInfo CreateDynamicTypeMapping(TexlBinding binding, TexlNode[] args)
        {
            Contracts.AssertValue(binding);
            Contracts.AssertValue(binding.EntityScope);

            if (Contracts.Verify(binding.EntityScope.TryGetEntity(new DName(binding.EntityName), out ControlInfo controlInfo)))
            {
                var dynamicTypeInfo = new DynamicTypeInfo((EntityScope)binding.Document.GlobalScope, Guid.NewGuid().ToString(), ComputeArgHash(args), controlInfo, binding.Property.Name, Name, ParentService.ServiceNamespace, null);
                dynamicTypeInfo.RegisterWithDocument();
                return dynamicTypeInfo;
            }
            return null;
        }
#endif

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            bool fArgsValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

#if canvas
            // Check if we have a dynamic type for a dynamic schema
            if (IsDynamic && binding.Document.Properties.EnabledFeatures.IsDynamicSchemaEnabled)
            {
                DType dynamicType = TryGetDynamicType(binding, args, out var dynamicTypeInfo) ? dynamicTypeInfo.GetReturnValueType() : new DType(DKind.ObjNull);
                if (dynamicType.Kind != DKind.ObjNull)
                {
                    returnType = dynamicType;
                }
            }
#endif
            return fArgsValid;
        }

#if canvas
        public override bool PostVisitValidation(TexlBinding binding, CallNode callNode)
        {
            if (Contracts.Verify((binding.Document as Document).TryGetServiceInfo(Namespace.Name, out ServiceInfo serviceInfo)) &&
                serviceInfo.Errors.Any(error => error.Severity >= DocumentErrorSeverity.Severe))
            {
                binding.ErrorContainer.EnsureError(callNode, CanvasStringResources.ErrInvalidService);
                return true;
            }
            return false;
        }

        public bool TryGetDynamicType(TexlBinding binding, TexlNode[] args, out DynamicTypeInfo dynamicTypeInfo)
        {
            if (FeatureGates.DocumentPreviewFlags.DynamicSchema)
            {
                // Map the property name to the DynamicTypeInfo, first mapping from a hash of the function args
                uint argHash = ComputeArgHash(args);
                dynamicTypeInfo = ((Document)binding.Document).GlobalScope.DynamicTypes.Cast<DynamicTypeInfo>().FirstOrDefault(entity => entity.Control.EntityName == binding.EntityName && entity.PropertyName == binding.Property.Name && entity.ArgHash == argHash);
                return dynamicTypeInfo != null;
            }

            dynamicTypeInfo = null;
            return false;
        }

        public override bool CheckForDynamicReturnType(TexlBinding binding, TexlNode[] args)
        {
            // Check if we have a dynamic type for a dynamic schema
            if (IsDynamic)
            {
                var dynamicKind = TryGetDynamicType(binding, args, out var dynamicTypeInfo) ? dynamicTypeInfo.GetReturnValueType().Kind : DKind.ObjNull;
                return (dynamicKind != DKind.ObjNull);
            }
            return false;
        }
#endif

        // This method returns true if there are special suggestions for a particular parameter of the function.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(0 <= argumentIndex);

            return argumentIndex <= MaxArity;
        }

        // Given the input type and the index of the argument, this function returns the acceptable suggestion string and type.
        public IEnumerable<KeyValuePair<string, DType>> GetServiceFunctionArgumentSuggestions(DType scopeType, int argumentIndex, out bool requiresSuggestionEscaping)
        {
            Contracts.Assert(scopeType.IsValid);
            Contracts.Assert(0 <= argumentIndex);

            requiresSuggestionEscaping = false;
            if (argumentIndex < MinArity)
            {
                Contracts.Assert(_orderedRequiredParams.Length > argumentIndex);
                return GetOptionSuggestions(_orderedRequiredParams[argumentIndex]);
            }

            return _optionalParamInfo.Select(x =>
                new KeyValuePair<string, DType>(TexlLexer.PunctuatorCurlyOpen + TexlLexer.EscapeName(x.Key) + TexlLexer.PunctuatorColon, x.Value.Type));
        }

        // Given the parameter name, this method returns the options available for the parameter, if any.
        public IEnumerable<KeyValuePair<string, DType>> GetOptionSuggestions(string paramName)
        {
            Contracts.AssertNonEmpty(paramName);

            if (!_parameterOptions.Any())
                return EnumerableUtils.Yield<KeyValuePair<string, DType>>();

            var option = _parameterOptions.Where(x => x.Key.Name.Value.Equals(paramName));
            Contracts.Assert(option.Count() <= 1);

            if (option.Count() == 0)
                return EnumerableUtils.Yield<KeyValuePair<string, DType>>();

            List<KeyValuePair<string, DType>> suggestions = new List<KeyValuePair<string, DType>>();
            var paramOptions = option.FirstOrDefault();
            DType paramType = paramOptions.Key.Type;
            string paramTypeString = paramType.ToString();

            foreach (var val in paramOptions.Value)
            {
                switch (paramTypeString)
                {
                    case "s":
                        suggestions.Add(new KeyValuePair<string, DType>("\"" + val + "\"", paramType));
                        break;
                    case "n":
                    case "b":
                        suggestions.Add(new KeyValuePair<string, DType>(val, paramType));
                        break;
                    default:
                        Contracts.Assert(false, "Parameter options should be of primitive type.");
                        break;
                }
            }

            return suggestions;
        }

        // Fetch the description associated with the specified parameter name.
        // If the param has no description, this will return false.
        public override bool TryGetParamDescription(string paramName, out string paramDescription)
        {
            Contracts.AssertNonEmpty(paramName);

            return _parameterDescriptionMap.TryGetValue(paramName, out paramDescription);
        }

        public bool TryGetParamDefaultValue(string paramName, out Tuple<string, DType> defaultValue)
        {
            Contracts.AssertValue(paramName);

            return _parameterDefaultValues.TryGetValue(paramName, out defaultValue);
        }

#if !canvas
        // Provide as hook for execution. 
        public IAsyncTexlFunction _invoker;

        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        {
            if (_invoker == null) 
            { 
                throw new InvalidOperationException($"Function {Name} can't be invoked."); 
            }

            var result = await _invoker.InvokeAsync(args, cancellationToken);
            ExpressionError er = null;

            if (result is ErrorValue ev && (er = ev.Errors.FirstOrDefault(e => e.Kind == ErrorKind.Network)) != null)
            {
                result = FormulaValue.NewError(
                    new ExpressionError()
                    {
                        Kind = er.Kind,
                        Severity = er.Severity,
                        Message = $"{Namespace.ToDottedSyntax()}.{Name} failed: {er.Message}"
                    },
                    ev.Type);
            }

            return result;
        }

        // Swap for IService, to cut dependency on TransportType.
        public class IService
        {
        }
#endif

#if canvas
        // Finishes JS generation for dynamic schemas
        public static bool TryPushCustomJsExpression(TexlFunction func, JsTranslator translator, CallNode node, List<Fragment> args, out Fragment fragment)
        {
            if (func.IsDynamic && translator.IsCapturingSchema)
            {
                fragment = translator.FinishXlatNonDelegatableCall(node, func, args, isDynamicSchema: true);
                return true;
            }

            fragment = null;
            return false;
        }*/
#endif
    }
}
