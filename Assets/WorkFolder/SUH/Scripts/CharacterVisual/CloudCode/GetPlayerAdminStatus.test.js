const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");
const path = require("node:path");
let items = [], calls = 0;
class DataApi {
    constructor(options) { assert.equal(options.accessToken, "server-token"); }
    async getProtectedItems(project, player, keys) {
        calls++;
        assert.equal(project, "project");
        assert.equal(player, "target");
        assert.deepEqual(Array.from(keys), ["IsAdmin"]);
        return { data: { results: items } };
    }
}
const sandbox = { module: { exports: {} }, require: () => ({ DataApi }) };
vm.runInNewContext(fs.readFileSync(path.join(__dirname, "GetPlayerAdminStatus.js"), "utf8"), sandbox);
const endpoint = sandbox.module.exports;
const request = { params: { targetPlayerId: "target" }, context: {
    projectId: "project", playerId: "caller", serviceToken: "server-token"
} };
(async () => {
    for (const [value, expected] of [[true, true], [false, false], ["true", false], [1, false], [null, false]]) {
        items = [{ key: "IsAdmin", value }];
        const result = await endpoint(request);
        assert.equal(result.playerId, "target");
        assert.equal(result.isAdmin, expected);
    }
    items = [];
    assert.equal((await endpoint(request)).isAdmin, false);
    const before = calls;
    await assert.rejects(() => endpoint({ ...request, context: {} }), /Authentication/);
    await assert.rejects(() => endpoint({ ...request, params: { targetPlayerId: "../invalid" } }), /Invalid/);
    assert.equal(calls, before);
    items = null;
    await assert.rejects(() => endpoint(request), /Invalid Cloud Save/);
    console.log("PASS: admin boolean, missing key, authentication, target validation, malformed response");
})().catch(error => { console.error(error); process.exitCode = 1; });
