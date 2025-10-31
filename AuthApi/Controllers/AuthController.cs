using AuthService.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.Controllers;

/// <inheritdoc />
[ApiController]
[Route("[controller]/[action]")]
public class AuthController(IAuthService service) : ControllerBase
{
    //#region MOBILE
    ///// <summary>
    /////  API`S OF MOBILE
    ///// </summary>
    ///// <param name="dto"></param>
    ///// <returns></returns>
    //[AllowAnonymous]
    //[HttpPost]
    //public async Task<Wrapper> RegisterMobile(MobileRegisterDto dto)
    //{
    //    return (await service.RegisterAsync(dto), 200);
    //}

    ///// <inheritdoc />
    //[AllowAnonymous]
    //[HttpPost]
    //public async Task<Wrapper> VerifyMobile(MobileVerifyOtpDto dto) =>
    //     (await service.VerifyOtpAfterLoginAsync(dto), 200);
    ///// <inheritdoc />
    //[AllowAnonymous]
    //[HttpGet]
    //public async Task<Wrapper> CheckEmailRegistered(string email) =>
    //     (await service.CheckPhoneRegisteredAsync(email), 200);
    //#endregion

}
//#region WEB
///// <summary>
///// API`S OF WEB
///// </summary>
///// <param name="dto"></param>
///// <returns></returns>
//[AllowAnonymous]
//[HttpPost]
//public async Task<Wrapper> Register(RegisterDto dto)
//{
//    await service.RegisterAsync(dto);
//    return 200;
//}
///// <inheritdoc />
//[AllowAnonymous]
//[HttpPost]
//public async Task<Wrapper> VerifyOtpAfterLogin(VerifyOtpDto dto) =>
//     (await service.VerifyOtpAfterLoginAsync(dto), 200);
//#endregion

//    #region WEB and MOBILE
//    /// <summary>
//    /// API`S OF WEB and MOBILE
//    /// </summary>
//    /// <param name="dto"></param>
//    /// <returns></returns>
//    [AllowAnonymous]
//    [HttpPost]
//    public async Task<Wrapper> Sign(SignDto dto) =>
//         (await service.SignAsync(dto), 200);
//    /// <inheritdoc />
//    [AllowAnonymous]
//    [HttpPost]
//    public async Task<Wrapper> GenerateTokenChoosenCompany(ChoosenCompanyDto dto) =>
//         (await service.GenerateTokenChoosenCompanyAsync(dto), 200);
//    /// <inheritdoc />
//    [HttpGet]
//    public async Task<Wrapper> GetMe()
//    {
//        return (await service.GetMeAsync(this.UserId, this.User.FindFirstValue(CustomClaimNames.CompanyId)), 200);

//        //return (await service.GetMeAsync(this.UserId), 200);
//    }
//    /// <inheritdoc />
//    [AllowAnonymous]
//    [HttpPut]
//    public async Task<Wrapper> ResetPassword([FromBody] ResetPasswordDto dto)
//    {
//        await service.ResetPasswordAsync(dto);
//        return 200;
//    }
//    /// <inheritdoc />
//    [HttpPost]
//    public async Task<Wrapper> Logout()
//    {
//        await service.LogoutAsync(this.UserId);
//        return 200;
//    }
//    /// <inheritdoc />
//    [AllowAnonymous]
//    [HttpPost]
//    public async Task<Wrapper> RefreshToken() =>
//         (await service.RefreshTokenAsync(), 200);
//    /// <inheritdoc />
//    [HttpPost]
//    public async Task<Wrapper> AddAnotherCompany([FromBody] AddAnotherCompanyDto dto)
//    {
//        await service.AddAnotherCompanyAsync(dto, UserId, CompanyId);
//        return 200;
//    }
//    /// <inheritdoc />
//    [HttpGet]
//    public async Task<Wrapper> MyCompanies() =>
//         (await service.MyCompaniesAsync(UserId, this.User.FindFirstValue(CustomClaimNames.CompanyId)), 200);
//    #endregion

//    #region OTHERS
//    /// <summary>
//    /// API`S OF OTHERS
//    /// </summary>
//    /// <param name="dto"></param>
//    /// <returns></returns>
//    [AllowAnonymous]
//    [HttpPost]
//    public async Task<Wrapper> VerifyOtpCode(VerifyOtpDto dto)
//    {
//        await service.VerifyOtpCodeAsync(dto, false);
//        return 200;
//    }
//    #endregion
//}
