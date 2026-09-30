const { DataApi } = require("@unity-services/cloud-save-1.4");

// Publish as GetPlayerAdminStatus with a required String parameter targetPlayerId.
module.exports = async ({ params, context }) => {
    const { projectId, playerId, serviceToken } = context;
    if (!playerId || !serviceToken) throw new Error("Authentication required");
    const targetPlayerId = params.targetPlayerId;
    if (typeof targetPlayerId !== "string" || !/^[a-zA-Z0-9_-]{1,128}$/.test(targetPlayerId)) {
        throw new Error("Invalid targetPlayerId");
    }
    const api = new DataApi({ accessToken: serviceToken });
    const response = await api.getProtectedItems(projectId, targetPlayerId, ["IsAdmin"]);
    const entries = response.data?.results;
    if (!Array.isArray(entries)) throw new Error("Invalid Cloud Save response");
    const entry = entries.find(item => item.key === "IsAdmin");
    return { playerId: targetPlayerId, isAdmin: entry?.value === true };
};
