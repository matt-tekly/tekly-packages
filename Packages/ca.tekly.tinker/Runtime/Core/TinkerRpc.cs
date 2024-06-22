using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Tekly.Tinker.Core
{
	[Route("")]
	public class TinkerRpc
	{
		private JsonSerializerSettings m_settings = new JsonSerializerSettings {
			ContractResolver = new CamelCasePropertyNamesContractResolver()
		};

		[Post("/rpc")]
		public void Handle(HttpListenerRequest request, HttpListenerResponse listenerResponse)
		{
			var rpcRequest = GetBodyAsType<RpcRequest>(request);

			if (rpcRequest.Method == "system.describe") {
				var description = new ResultDescribe();
				description.Add("logs", "it is logs")
					.Params.Add("tacos");
				
				var json = JsonConvert.SerializeObject(description, Formatting.Indented, m_settings);
				listenerResponse.WriteText(json, "application/json"); 
			} else {

				var rpcResponse = new RpcResponse();
				rpcResponse.Id = rpcRequest.Id;
				rpcResponse.Result = rpcRequest.Method;
				
				var json = JsonConvert.SerializeObject(rpcResponse, Formatting.Indented, m_settings);
				listenerResponse.WriteText(json, "application/json"); 
			}
		}
		
		public T GetBodyAsType<T>(HttpListenerRequest request)
		{
			using var bodyStream = new StreamReader(request.InputStream);
			var requestBody = bodyStream.ReadToEnd();

			return JsonConvert.DeserializeObject<T>(requestBody, m_settings);
		}
	}

	public class RpcRequest
	{
		public string Method;
		public List<object> Params = new List<object>();
		public object Id;
	}

	public class RpcResponse
	{
		public string Jsonrpc = "2.0";
		public object Result;
		public object Id;
	}

	public class Result
	{
		public string Name;
	}

	public class ResultDescribe : Result
	{
		public List<Procedure> Procs = new List<Procedure>();

		public Procedure Add(string name, string summary)
		{
			var procedure = new Procedure();
			procedure.Name = name;
			procedure.Summary = summary;

			procedure.Return = new ProcedureParam("result", "string", "it is a string");

			Procs.Add(procedure);
			return procedure;
		}
	}

	public class Procedure
	{
		public string Name;
		public string Summary;

		public List<string> Params = new List<string>();
		public ProcedureParam Return;
	}

	public class ProcedureParam
	{
		public ProcedureParam(string name, string type, string summary)
		{
			Name = name;
			Type = type;
			Summary = summary;
		}

		public string Name;
		public string Type;
		public string Summary;
	}
}