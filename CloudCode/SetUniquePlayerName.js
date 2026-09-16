const { DataApi } = require("@unity-services/cloud-save-1.4");

module.exports = async ({ params, context, logger }) => {
    const requestedName = params.requestedName;
    if (typeof requestedName !== "string" || !requestedName.trim()) {
        throw new Error("Bad Request: requestedName is missing");
    }
    const playerName = requestedName.trim();
    const { projectId, playerId, serviceToken } = context;
    if (!playerId || !serviceToken) throw new Error("Authenticated player and service token required");
    const api = new DataApi({ accessToken: serviceToken });
    try {
        // Requires a Default Player Data index: PlayerName, ascending.
        const search = await api.queryDefaultPlayerData(projectId, {
            fields: [{ key: "PlayerName", op: "EQ", value: playerName, asc: true }],
            limit: 2
        });
        const results = search.data?.results;
        if (!Array.isArray(results) || results.some(item => typeof item.id !== "string")) {
            throw new Error("Unexpected Cloud Save query response");
        }
        if (results.some(item => item.id !== playerId)) return { result: "Conflict" };

        await api.setItem(projectId, playerId, { key: "PlayerName", value: playerName });
        return { result: "Success", playerName };
    } catch (error) {
        logger.error(`SetUniquePlayerName failed: ${error.message}`);
        throw error;
    }
};
