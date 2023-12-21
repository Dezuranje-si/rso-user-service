﻿using Carter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using RSO.Core.BL;
using RSO.Core.BL.LogicModels;
using RSO.Core.UserModels;

namespace UserServiceRSO.CarterModules;

public class UserEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // Login and register options.
        app.MapPost("/login", Login).WithName(nameof(Login)).
            Produces(StatusCodes.Status200OK).WithDisplayName("Lol").
            Produces(StatusCodes.Status400BadRequest).
            AllowAnonymous().WithTags("Users");

        app.MapPost("/register", Register).WithName(nameof(Register)).
            Produces(StatusCodes.Status201Created).
            Produces(StatusCodes.Status400BadRequest).
            Produces(StatusCodes.Status404NotFound).WithDescription("Ne najdem").
            AllowAnonymous().WithTags("Users");

        // Group for /api/user endpoinds.
        var group = app.MapGroup("/api/user/");

        // Methods for  /api/user/id endpoints.
        group.MapGet("{id}", GetUserById).WithName(nameof(GetUserById)).
            Produces(StatusCodes.Status200OK).
            Produces(StatusCodes.Status400BadRequest).
            Produces(StatusCodes.Status401Unauthorized).WithTags("Users");
    }

    /// <summary>
    /// Performs login and gets the JWT token.
    /// </summary>
    /// <param name="emailOrUsername"></param>
    /// <param name="password"></param>
    /// <param name="userLogic"></param>
    /// <returns>A JWT token as a string.</returns>
    [AllowAnonymous]
    public static async Task<Results<Ok<string>, BadRequest<string>>> Login(string emailOrUsername, string password, IUserLogic userLogic)
    {
        if (string.IsNullOrEmpty(emailOrUsername) || string.IsNullOrEmpty(password))
            return TypedResults.BadRequest("Username (or email) and password cannot be null");
        else
        {
            var user = await userLogic.GetUserByUsernameOrEmailAndPasswordAsync(emailOrUsername, password);
            var jwt = userLogic.GetJwtToken(user);
            return jwt is null
                ? TypedResults.BadRequest("The user with the specified username/email and password doesn't exist.")
                : TypedResults.Ok(jwt);
        }
    }

    /// <summary>
    /// Performs registration and gets the JWT token.
    /// </summary>
    /// <param name="newUser">Data for the new user that is going to be created.</param>
    /// <param name="userLogic">DI for B(usiness) L(logic) layer.</param>
    /// <returns>A JWT token as a string.</returns>
    public static async Task<Results<Created<string>, NotFound<string>, BadRequest<string>>> Register( User newUser, IUserLogic userLogic)
    {
        //SKIP VALIDATION (IMPLEMENT IT WHEN TIME REMAINS

        newUser.UserCity = await userLogic.GetCityFromZipCodeAsync(newUser.UserZipCode);
        newUser.UserZipCode = string.IsNullOrEmpty(newUser.UserCity) ? null : newUser.UserZipCode;

        //Insert user and create a logic for the JWT
        try
        {
            // Inser user into database
            var user = await userLogic.RegisterUserAsync(newUser);
            if (user is null) return TypedResults.NotFound("Something happened with the database that prevented the insertion of the user.");
            // Generate a JWT token.
            var jwt = userLogic.GetJwtToken(user);
            return string.IsNullOrEmpty(jwt) ? TypedResults.BadRequest("User has been successfully registered but failed to retrieve the JWT token.") : TypedResults.Created("/", jwt);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Gets the user by id.
    /// </summary>
    /// <param name="id">Id of the user.</param>
    /// <param name="userLogic"><see cref="IUserLogic"/> instance.</param>
    /// <returns>User data for the user.</returns>
    public static async Task<Results<Ok<UserDataDTO>, BadRequest<string>>> GetUserById(int id, IUserLogic userLogic)
    {
        var user = await userLogic.GetUserByIdAsync(id);

        if (user is null)
            return TypedResults.BadRequest("User with the specified doesn't exist.");
        var userData = new UserDataDTO(user);

        return TypedResults.Ok(userData);
    }
}
