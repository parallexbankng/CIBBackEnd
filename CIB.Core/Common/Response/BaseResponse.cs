using System;
using System.Collections.Generic;
using FluentValidation.Results;

namespace CIB.Core.Common.Response
{
	public class ResponseDTO<T>
	{
		public ResponseDTO(T _data, bool success, string _message)
		{
			Message = _message;
			Data = _data;
			Success = success;
		}
		public string Message { get; set; }
		public bool Success { get; set; }
		public T Data { get; set; }
	}
	public class ListResponseDTO<T>
	{
		public ListResponseDTO(List<T> _data, bool success, string _message)
		{
			Message = _message;
			Data = _data;
			Success = success;
		}
		public string Message { get; set; }
		public bool Success { get; set; }
		public List<T> Data { get; set; }
	}
	public class ValidatorResponse
	{
		public ValidatorResponse(object _data, bool _success, List<ValidationFailure> _validationResult)
		{
			var errorList = new Dictionary<string, string>();
			foreach (var error in _validationResult)
			{
				if (!errorList.ContainsKey(error.PropertyName))
				{
					errorList.Add(error.PropertyName, error.ErrorMessage);
				}
			}
			Data = _data;
			Errors = errorList;
			Success = _success;
			Message = "Validation Error";
		}

		public object Data { get; set; }
		public bool Success { get; set; }
		public string Message { get; set; }
		public Dictionary<string, string> Errors { get; set; }
	}
}