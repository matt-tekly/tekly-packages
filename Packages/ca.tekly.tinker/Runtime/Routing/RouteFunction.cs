#if UNITY_EDITOR && TINKER_ENABLED_EDITOR
#define TINKER_ENABLED
#endif

#if TINKER_ENABLED
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Tekly.Tinker.Core;
using Tekly.Tinker.Http;

namespace Tekly.Tinker.Routing
{
	public class FunctionParameter
	{
		public string Name { get; }
		public string DisplayName { get; }

		[JsonIgnore] public Type ActualType { get; }
		public bool Optional { get; }
		public string DefaultValue { get; }
		public string[] Values { get; }
		public string EditType { get; }

		public FunctionParameter(ParameterInfo param)
		{
			Name = param.Name;
			DisplayName = RouteUtils.NiceName(Name);
			ActualType = param.ParameterType;
			Optional = param.IsOptional;

			DefaultValue = param.DefaultValue != null ? param.DefaultValue.ToString() : "";

			if (param.ParameterType.IsEnum) {
				Values = Enum.GetNames(param.ParameterType);
				if (string.IsNullOrEmpty(DefaultValue)) {
					DefaultValue = Values[0];
				}

				EditType = "select";
			} else if (RouteUtils.IsNumber(param.ParameterType)) {
				EditType = "number";
			} else if (param.ParameterType == typeof(string)) {
				if (param.GetAttribute<LargeTextAttribute>() != null) {
					EditType = "textarea";
				} else {
					EditType = "text";
				}
			} else if (param.ParameterType == typeof(bool)) {
				EditType = "checkbox";
			}
		}
	}

	public class RouteFunction
	{
		public bool Visible => m_descriptionAttribute != null;
		public string DisplayName => m_descriptionAttribute?.Name;
		public string Description => m_descriptionAttribute?.Description;
		public FunctionParameter[] Parameters => m_parameters;
		public string Path => m_path;
		public string Verb => m_verb;
		public string Id { get; }

		public bool IsCommand => m_commandAttribute != null;
		public string CommandName => m_commandAttribute?.Name;

		private readonly TinkerServer m_tinkerServer;

		private readonly MethodInfo m_method;
		private readonly string m_path;
		private readonly string m_verb;

		private readonly FunctionParameter[] m_parameters;
		private readonly DescriptionAttribute m_descriptionAttribute;
		private readonly PageAttribute m_pageAttribute;
		private readonly CommandAttribute m_commandAttribute;

		public RouteFunction(MethodInfo method, string root, TinkerServer tinkerServer)
		{
			m_method = method;
			m_tinkerServer = tinkerServer;

			m_descriptionAttribute = m_method.GetAttribute<DescriptionAttribute>();
			m_pageAttribute = m_method.GetAttribute<PageAttribute>();
			m_commandAttribute = m_method.GetAttribute<CommandAttribute>();

			var route = method.GetAttribute<RouteAttribute>();

			m_path = root + route.Route;
			m_verb = route.Verb;

			Id = m_path.Replace("/", "_");

			m_parameters = method.GetParameters()
				.Select(x => new FunctionParameter(x))
				.ToArray();
		}
		
		public bool Matches(HttpListenerRequest request)
		{
			return request.HttpMethod == m_verb && request.Url.LocalPath == m_path;
		}

		public void Invoke(object instance, HttpListenerRequest request, HttpListenerResponse response)
		{
			response.Headers.Add("Access-Control-Allow-Origin", "*");
			HandleInvokeAsync(instance, request, response);
		}

		private async void HandleInvokeAsync(object instance, HttpListenerRequest request, HttpListenerResponse response)
		{
			var invokeParams = GetInvokeParams(request, response);
			var result = m_method.Invoke(instance, invokeParams);

			try {
				result = await HandleTask(result);
			
				if (m_pageAttribute != null) {
					var content = m_tinkerServer.RenderPage(m_path, m_pageAttribute.TemplateName, m_pageAttribute.DataKey, result);
					response.WriteHtml(content);
				} else if (m_method.ReturnType == typeof(string)) {
					response.WriteText(result.ToString());
				} else if (m_method.ReturnType == typeof(HtmlContent)) {
					response.WriteHtml(result.ToString());
				} else if (m_method.ReturnType == typeof(void)) {
					// Do nothing
				} else {
					using var sw = new StreamWriter(response.OutputStream);
					using var writer = new JsonTextWriter(sw);
					writer.Formatting = Formatting.Indented;

					response.ContentEncoding = Encoding.UTF8;
					response.ContentType = "application/json";
					m_tinkerServer.Serializer.Serialize(writer, result);
				}
			} catch (Exception e) {
				response.StatusCode = (int)HttpStatusCode.InternalServerError;
				response.WriteText(e.ToString());
			}
		}

		private async Task<object> HandleTask(object invokeResult)
		{
			if (invokeResult == null) {
				return null;
			}
			
			var taskType = invokeResult.GetType();

			if (invokeResult is Task task) {
				await task;
				
				var resultProperty = taskType.GetProperty("Result");
				if (resultProperty != null) {
					var taskValue = resultProperty.GetValue(task);
					return taskValue;
				}
				
				return null;
			}

			return invokeResult;
		}

		private object[] GetInvokeParams(HttpListenerRequest request, HttpListenerResponse response)
		{
			var queryParams = request.GetQueryParams();
			var invokeParams = new object[m_parameters.Length];

			for (var index = 0; index < m_parameters.Length; index++) {
				var parameter = m_parameters[index];

				if (parameter.ActualType == typeof(HttpListenerRequest)) {
					invokeParams[index] = request;
					continue;
				}

				if (parameter.ActualType == typeof(HttpListenerResponse)) {
					invokeParams[index] = response;
					continue;
				}

				if (parameter.ActualType == typeof(TinkerServer)) {
					invokeParams[index] = m_tinkerServer;
					continue;
				}
				
				if (parameter.ActualType == typeof(CancellationToken)) {
					invokeParams[index] = m_tinkerServer.CancellationToken;
					continue;
				}

				var value = queryParams.Get(parameter.Name);

				if (value != null) {
					invokeParams[index] = RouteUtils.Parse(parameter.ActualType, value);
				} else if (parameter.Optional) {
					invokeParams[index] = RouteUtils.Parse(parameter.ActualType, parameter.DefaultValue);
				} else {
					throw new Exception($"Query missing required parameter: {parameter.Name}");
				}
			}

			return invokeParams;
		}
	}
}
#endif