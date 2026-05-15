namespace ATOZA.Domain.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string entityName, object key)
            : base($"'{entityName}' với id '{key}' không tìm thấy.") { }
    }

    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message = "Bạn không có quyền thực hiện thao tác này.")
            : base(message) { }
    }

    public class DuplicateEntityException : Exception
    {
        public DuplicateEntityException(string message)
            : base(message) { }
    }

    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message)
            : base(message) { }
    }
}
