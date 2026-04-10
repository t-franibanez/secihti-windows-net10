namespace SECIHTI.Models.Common
{
    public class Response<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public Response(T data, string message = "OK")
        {
            Success = true;
            Message = message;
            Data = data;
        }

        public Response(string errorMessage)
        {
            Success = false;
            Message = errorMessage;
            Data = default;
        }
    }
}
