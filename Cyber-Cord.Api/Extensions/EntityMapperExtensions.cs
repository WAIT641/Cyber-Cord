using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Models;

namespace Cyber_Cord.Api.Extensions;

public static class EntityMapperExtensions
{
    public static UserReturnModel ToReturnModel(this User user)
    {
        return new UserReturnModel
        {
            Id = user.Id,
            Name = user.UserName!,
            DisplayName = user.DisplayName,
            Description = user.Description,
            CreatedAt = user.CreatedAt,
            BannerColor = new ColorReturnModel
            {
                Red = user.BannerColor.R,
                Green = user.BannerColor.G,
                Blue = user.BannerColor.B,
            }
        };
    }
    
    public static MessageReturnModel ToReturnModel(this Message message)
    {
        return new MessageReturnModel
        {
            Id = message.Id,
            UserId = message.UserId,
            Content = message.Content,
            CreatedAt = message.CreatedAt,
            ChannelId = message.ChannelId,
            ChatId = message.ChatId
        };
    }
}
