function hashLoginPassword(doForm, cur_session_id)
{
	// Compatibility.
	if (cur_session_id == null)
		cur_session_id = smf_session_id;

	if (typeof(hex_sha1) == "undefined")
		return;

	// Unless the browser is Opera, the password will not save properly.
	if (typeof(window.opera) == "undefined")
		doForm.userpw.autocomplete = "off";

	doForm.hash_passwrd.value = hex_sha1(hex_sha1(doForm.username.value.toLowerCase() + doForm.userpw.value) + cur_session_id);

	// It looks nicer to fill it with asterisks, but Firefox will try to save that.
	if (navigator.userAgent.indexOf("Firefox/") != -1)
		doForm.userpw.value = "";
	else
		doForm.userpw.value = doForm.userpw.value.replace(/./g, "*");
}

function hashAdminPassword(doForm, username, cur_session_id)
{
	// Compatibility.
	if (cur_session_id == null)
		cur_session_id = smf_session_id;

	if (typeof(hex_sha1) == "undefined")
		return;

	doForm.admin_hash_pass.value = hex_sha1(hex_sha1(username.toLowerCase() + doForm.admin_pass.value) + cur_session_id);
	doForm.admin_pass.value = doForm.admin_pass.value.replace(/./g, "*");
}