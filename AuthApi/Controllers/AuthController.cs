using AuthService.Contracts.Auth;
using AuthService.Contracts.Auth.Both;
using AuthService.Interfaces;
using Common.ResultWrapper.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.Design;
using System.Security.Claims;

namespace AuthApi.Controllers;

/// <inheritdoc />
[ApiController]
[Route("[controller]/[action]")]
public class AuthController(IAuthService service) : ControllerBase
{
    #region MOBILE
    /// <summary>
    ///  API`S OF MOBILE
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpPost]
    public async Task<Wrapper> RegisterMobile(MobileRegisterDto dto)
    {
        return (await service.RegisterAsync(dto), 200);
    }

    /// <inheritdoc />
    [AllowAnonymous]
    [HttpPost]
    public async Task<Wrapper> VerifyMobile(MobileVerifyOtpDto dto) =>
         (await service.VerifyOtpAfterLoginAsync(dto), 200);
    /// <inheritdoc />
    [AllowAnonymous]
    [HttpGet]
    public async Task<Wrapper> CheckEmailRegistered(string email) =>
         (await service.CheckPhoneRegisteredAsync(email), 200);
    #endregion

    #region WEB
    /// <summary>
    /// API`S OF WEB
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpPost]
    public async Task<Wrapper> Register(RegisterDto dto)
    {
        await service.RegisterAsync(dto);
        return 200;
    }
    /// <inheritdoc />
    [AllowAnonymous]
    [HttpPost]
    public async Task<Wrapper> VerifyOtpAfterLogin(VerifyOtpDto dto) =>
         (await service.VerifyOtpAfterLoginAsync(dto), 200);
    #endregion

    #region WEB and MOBILE
    /// <summary>
    /// API`S OF WEB and MOBILE
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpPost]
    public async Task<Wrapper> Sign(SignDto dto) =>
         (await service.SignAsync(dto), 200);
    /// <inheritdoc />
    [AllowAnonymous]
    [HttpPost]
    public async Task<Wrapper> GenerateTokenChoosenCompany(ChoosenCompanyDto dto) =>
         (await service.GenerateTokenChoosenCompanyAsync(dto), 200);
    /// <inheritdoc />
    [HttpGet]
    public async Task<Wrapper> GetMe()
    {
        return (await service.GetMeAsync(this.UserId, this.User.FindFirstValue(CustomClaimNames.CompanyId)), 200);

        //return (await service.GetMeAsync(this.UserId), 200);
    }
    /// <inheritdoc />
    [AllowAnonymous]
    [HttpPut]
    public async Task<Wrapper> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        await service.ResetPasswordAsync(dto);
        return 200;
    }
    /// <inheritdoc />
    [HttpPost]
    public async Task<Wrapper> Logout()
    {
        await service.LogoutAsync(this.UserId);
        return 200;
    }
    /// <inheritdoc />
    [AllowAnonymous]
    [HttpPost]
    public async Task<Wrapper> RefreshToken() =>
         (await service.RefreshTokenAsync(), 200);
    /// <inheritdoc />
    [HttpPost]
    public async Task<Wrapper> AddAnotherCompany([FromBody] AddAnotherCompanyDto dto)
    {
        await service.AddAnotherCompanyAsync(dto, UserId, CompanyId);
        return 200;
    }
    /// <inheritdoc />
    [HttpPost]
    public async Task<Wrapper> IntoSign([FromBody] ChoosenCompanyDto dto) =>
         (await service.IntoSignAsync(dto, UserId), 200);
    /// <inheritdoc />
    [HttpGet]
    public async Task<Wrapper> MyCompanies() =>
         (await service.MyCompaniesAsync(UserId, this.User.FindFirstValue(CustomClaimNames.CompanyId)), 200);
    #endregion

    #region EMPLOYEE
    /// <summary>
    /// API`S OF EMPLOYEE
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet]
    [BRB.Core.Web.Attributes.Authorize((int)EnumPermissions.GetEmployees)]
    public async Task<Wrapper> GetEmployees([FromQuery] DataQueryRequest request) =>
         await service.GetEmployeesAsync(request, CompanyId);
    /// <inheritdoc />
    [HttpPost]
    [BRB.Core.Web.Attributes.Authorize((int)EnumPermissions.AddEmployee)]
    public async Task<Wrapper> AddEmployee(AddEmployeeDto dto) =>
         (await service.AddEmployeeAsync(dto, CompanyId), 200);
    /// <inheritdoc />
    [HttpDelete]
    [BRB.Core.Web.Attributes.Authorize((int)EnumPermissions.DeleteEmployee)]
    public async Task<Wrapper> DeleteEmployee(long userId)
    {
        await service.DeleteEmployeeAsync(CompanyId, userId);
        return 200;
    }
    /// <inheritdoc />
    [HttpPut]
    [BRB.Core.Web.Attributes.Authorize((int)EnumPermissions.AssignRoleOfEmployee)]
    public async Task<Wrapper> AssignRoleOfEmployee(AssignRoleOfEmployeeDto dto)
    {
        await service.AssignRoleOfEmployeeAsync(dto);
        return 200;
    }
    #endregion

    #region DEVICE
    /// <summary>
    /// API`S OF DEVICE
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<Wrapper> GetTrustedDevices([FromQuery] DataQueryRequest request) =>
         await service.GetTrustedDevicesAsync(request, this.CompanyId);
    /// <inheritdoc />
    [HttpDelete]
    public async Task<Wrapper> RemoveDevice(long deviceId) =>
         await service.RemoveDeviceAsync(this.CompanyId, deviceId);
    #endregion

    #region OTHERS
    /// <summary>
    /// API`S OF OTHERS
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpPost]
    public async Task<Wrapper> VerifyOtpCode(VerifyOtpDto dto)
    {
        await service.VerifyOtpCodeAsync(dto, false);
        return 200;
    }
    /// <inheritdoc />
    [HttpPut]
    [AllowAnonymous]
    public async Task<Wrapper> FetchClientInfo([FromBody] string[] inns)
    {
        await service.FetchClientInfoAsync(inns);
        return 200;
    }
    /// <inheritdoc />
    [HttpGet]
    public async Task<Wrapper> GetClientInfoFromIabs(string inn) =>
         (await service.GetBankClientAsync(inn, true), 200);
    #endregion
}
