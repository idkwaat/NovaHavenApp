const configuredAddress = process.env.NEXT_PUBLIC_MINECRAFT_IP?.trim() ?? '';

export const minecraftServerAddress =
  /^[a-z0-9](?:[a-z0-9.-]*[a-z0-9])?(?::\d{1,5})?$/i.test(configuredAddress) &&
  !/(?:example\.invalid|localhost|127\.0\.0\.1|replace)/i.test(configuredAddress)
    ? configuredAddress
    : null;

const configuredDiscordInvite = process.env.NEXT_PUBLIC_DISCORD_URL?.trim() ?? '';
export const discordInviteUrl =
  /^https:\/\/(?:discord\.gg\/[a-z0-9-]+|discord\.com\/invite\/[a-z0-9-]+)\/?$/i.test(configuredDiscordInvite)
    ? configuredDiscordInvite
    : null;
