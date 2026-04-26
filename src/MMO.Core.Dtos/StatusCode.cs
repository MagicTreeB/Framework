
namespace MMO.Core.Dtos;

public enum StatusCode
{
    #region Prefixed with "2"
    Success = 200,

    Created = 201,
    Accepted = 202,

    #endregion

    #region Prefixed with "4"
    BadRequest = 400,
    NotFound = 404,
    Unauthorized = 401,
    Forbidden = 403,

    #endregion

    #region Prefixed with "5"
    InternalServerError = 500

    #endregion
}
