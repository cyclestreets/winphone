<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/engine/login.php $
 * $Revision: 50 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-12-20 20:32:52 +0000 (Sat, 20 Dec 2008) $
 *********************************************************/

$loader = new login();

class login
{
	var $html;
	var $output;
	
	function login()
	{
	    global $IN, $OUTPUT, $std;

        $std->AssertUsingFullVersion();

		$this->html = $OUTPUT->load_template("skin_login");
		switch($IN["ACT"])
		{
			case 'logout':
			$this->logout();
			break;

			case 'dologin':
			$this->dologin();
			break;

			default:
			$this->showlogin();
			break;
		}
		$OUTPUT->add_output($this->output);
		
	}
	
	function showlogin()
	{
		global $std, $guser, $IN, $CONFIG;
		
        $std->updateNav(" > ".GETLANG("nav_login"), 0);
		$data = array();
		if ( $IN["referer"] )
			$data["referer"] = $IN["referer"];
		else
			$data["referer"] = "index.php";
	    $data["posturl"] = "index.php?ACT=dologin";
		$data['loginCallback'] = $guser->userdb_loginCallback();
	    $this->output .= $this->html->login_form($data);

	}

	function dologin()
	{
	    global $CONFIG, $IN, $std;

        $std->updateNav(" > ".GETLANG("nav_login"), 0);
        if ( $CONFIG["usertype"] == "vb3" || $CONFIG["usertype"] == "vb35")
        {
            foreach( $IN as $t=>$u )
            {
                $IN[$t] = $std->undoHTMLChars($u);
            }
        }
	    $liuser = new user();
		$liuser->initialise();
	    $liuser->do_login();
	    if ($liuser->valid)
	    {
            $liuser->setusercookie();
            $data['referurl'] = $IN["referurl"];
			$data['message'] = GETLANG("login_ok");
			$this->output .= $this->html->login_redirect($data);
	    }
		else
		{
			$std->error(GETLANG("er_loginfailed").$liuser->errormsg);
		}
	}

	function logout()
	{
	    global $std, $IN, $CONFIG;
	    
	    $std->rw_setcookie("userid", "0");
	    $std->rw_setcookie("password", "0");
	    $std->rw_setcookie("username", "0");
	    $std->rw_setcookie("userlevel", "0");

        $std->updateNav(" > ".GETLANG("nav_logout"), 0);

		if ( $IN["referer"] )
			$data['referurl'] = $IN["referer"];
		else
			$data['referurl'] = "index.php";

		$data['message'] = GETLANG("logout_ok");
		$this->output .= $this->html->login_redirect($data);
	}
}
?>