<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/engine/userdb/vb3.php $
 * $Revision: 50 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-12-20 20:32:52 +0000 (Sat, 20 Dec 2008) $
 *********************************************************/

require_once ROOT_PATH."/engine/users.php";
 
class vb3database
{
	var $parent;
	var $name	= "VBulletin 3.x";
    var $version = 100;
	
	function init(&$parent)
	{
		global $CONFIG;
		
		$this->parent = &$parent;
		$this->parent->mem_table = "";
		$this->parent->mem_gtable = "";

		$this->parent->db_id = "userid";
		$this->parent->db_name = "username";
		$this->parent->db_password = "password";
		$this->parent->db_pass_salt = "salt";
		$this->parent->db_level = "usergroupid";
		$this->parent->db_email = "email";
		$this->parent->db_g_id = "usergroupid";
		$this->parent->db_g_title = "title";

		$this->parent->reg_link = $CONFIG["userfileurl"]."/register.php";
		$this->parent->admin_link = $CONFIG["userfileurl"]."/admincp/index.php";

	}

	function getDetails($configpath)
	{
		global $CONFIG, $servername,$dbusername,$dbpassword,$dbname,$tableprefix,$std;

        if ( $CONFIG['usermode'] == "manual" )
        {
            $dbinfo = array("sqlhost" => $CONFIG['userhost'],
    						"sqlusername" => $CONFIG['useruser'],
    						"sqlpassword" => $CONFIG['userpass'],
    						"sqldatabase" => $CONFIG['usertable'],
    						"sql_tbl_prefix" => $CONFIG['userprefix']);

    		$this->parent->userdb = new mysql($dbinfo);
    		$this->parent->mem_table = $CONFIG['userprefix']."user";
    		$this->parent->mem_gtable = $CONFIG['userprefix']."usergroup";
            return TRUE;
        }
        else
        {
    		if ( file_exists($configpath."/includes/config.php") )
    		    require_once ($configpath."/includes/config.php");
    		else
    		{
    			$this->parent->errormsg = "No configuration file was found in the location specified. Current user db is set to {$this->name}. We were expecting to find the file at {$configpath}/includes/config.php";
    			return false;
    		}
    		$dbinfo = array("sqlhost" => $servername,
    						"sqlusername" => $dbusername,
    						"sqlpassword" => $dbpassword,
    						"sqldatabase" => $dbname,
    						"sql_tbl_prefix" => $tableprefix);

    		$this->parent->userdb = new mysql($dbinfo);

    		$this->parent->mem_table = $tableprefix."user";
    		$this->parent->mem_gtable = $tableprefix."usergroup";
        }
		return true;
	}
	
	function do_login($username, $password)
	{
		global $IN, $DB, $CONFIG, $std;

        // First pass password encryption
		$password = md5($password);

		// Get user details
		$this->parent->userdb->query(  "SELECT g.{$this->parent->db_g_id}, g.{$this->parent->db_g_title},
										 u.{$this->parent->db_id},u.{$this->parent->db_name},u.{$this->parent->db_level},
										 u.{$this->parent->db_password}, u.{$this->parent->db_email}
								FROM `{$this->parent->mem_table}` u
								LEFT JOIN `{$this->parent->mem_gtable}` g ON (g.{$this->parent->db_g_id}=u.{$this->parent->db_level})
								WHERE u.{$this->parent->db_name}='$username'");
		
		$name = $this->parent->userdb->fetch_row();
		
		// Success?
		if ( empty($name[$this->parent->db_id]) || ($name[$this->parent->db_id] == "") )
		{
			$std->error("There was an error locating the details for this user");
			return false;
		}
		
		if (!$this->vb3_login($name, $password))
		    return false;
		
		return $name;
	}

	// ============================================
	// VB3 login specific stuff
	// ============================================
	function vb3_login($name, $password)
	{
        global $std;

		$id = $name[$this->parent->db_id];
        $this->parent->userdb->query(  "SELECT * FROM `{$this->parent->mem_table}` WHERE {$this->parent->db_id}='$id'");
        $converge = $this->parent->userdb->fetch_row();
        if ( empty($converge) )
        {
            $this->parent->errormsg = GETLANG("er_invaliduser");
            return FALSE;
        }

        // Validate password
        if ( !$converge['salt'] )
		{
            $this->parent->errormsg = GETLANG("er_nomatch");
			return FALSE;
		}

        if ( $converge[$this->parent->db_password] == md5($password.$converge[$this->parent->db_pass_salt]))
        	return true;
		else
		{
			$this->parent->errormsg = GETLANG("er_nomatch");
			return false;
		}
	}
	
	function auto_login($userid = -1, $pass = "password")
	{
		global $DB, $IN, $CONFIG, $std;
		
		$sql = "SELECT g.{$this->parent->db_g_id}, g.{$this->parent->db_g_title},
						 u.{$this->parent->db_id},u.{$this->parent->db_name},u.{$this->parent->db_level},
						 u.{$this->parent->db_password}, u.{$this->parent->db_email}
						FROM `{$this->parent->mem_table}` u
						LEFT JOIN `{$this->parent->mem_gtable}` g ON (g.{$this->parent->db_g_id}=u.{$this->parent->db_level})
						WHERE u.{$this->parent->db_id}='$userid' AND u.{$this->parent->db_password}='$pass'";
		// Get user details
		$this->parent->userdb->query( $sql );

		if ($myrow = $this->parent->userdb->fetch_row())
		    return $myrow;
		else
		    return false;
		
	}
	
		
	function login($username, $password)
	{
		global $CONFIG, $DB;
			
		$result = $this->parent->userdb->query(  "SELECT g.{$this->parent->db_g_id}, g.{$this->parent->db_g_title},
						    u.{$this->parent->db_id},u.{$this->parent->db_name},u.{$this->parent->db_level},
						    u.{$this->parent->db_password}, u.{$this->parent->db_email}
						    FROM `{$this->parent->mem_table}` u
						    LEFT JOIN `{$this->parent->mem_gtable}` g ON (g.{$this->parent->db_g_id}=u.{$this->parent->db_level})
						    WHERE u.{$this->parent->db_name}='$username' AND u.{$this->parent->db_password}='$password'");

		if ($myrow = $this->parent->userdb->fetch_row($result))
		{
			$result2 = $DB->query("SELECT * FROM `dl_memberextra` WHERE mid={$myrow[$this->parent->db_g_id]}");
			$myrow2 = $DB->fetch_row($result2);
			$result3 = $DB->query("SELECT * FROM `dl_groupsextra` WHERE geid={$myrow[$this->parent->db_level]}");
			$myrow3 = $DB->fetch_row($result3);
			
			if ( $myrow2 )
			    $myrow = array_merge($myrow,$myrow2);
			if ( $myrow3 )
			    $myrow = array_merge($myrow,$myrow3);
			return $myrow;
		}
		else
		    return false;
		
	}

	function uservalidate()
	{
		global $CONFIG, $IN, $DB, $std;

		$uid = $std->rw_getcookie("bbuserid");
		$passhash = $std->rw_getcookie("bbpassword");
		if ($uid)
		{
			if (!$this->parent->auto_login( $uid, $passhash ))
			{
				$this->parent->username = "Guest User";
				$this->parent->userlevel = $CONFIG["guestid"];
				$this->parent->isGuest = TRUE;

				$result = $DB->query("SELECT * FROM `dl_groupsextra` WHERE geid={$CONFIG['guestid']}");
				$this->parent->userdetails = $DB->fetch_row();

				$std->error(GETLANG("er_duffcookie"));
				
				// Not removing this cookie because its not ours
				/*$std->rw_setcookie("rwd_userid", "0");
				$std->rw_setcookie("rwd_password", "0");
				$std->rw_setcookie("rwd_username", "0");
				$std->rw_setcookie("rwd_userlevel", "0");*/
				
				return false;
			}
    		}
		else
		{
			$this->parent->username = "Guest User";
			$this->parent->userlevel = $CONFIG['guestid'];
			$result = $DB->query("SELECT * FROM `dl_groupsextra` WHERE geid={$CONFIG['guestid']}");
			$this->parent->userdetails = $DB->fetch_row();
			$this->parent->isGuest = TRUE;
			return false;
		}
	}

    function userdb_js()
    {
        return "";
    }

    function userdb_loginCallback()
    {
        return  "";
    }

    function setusercookie()
    {
        global $std, $IN;
        $std->rw_setcookie("userid", $this->parent->userid, $IN["remember"]);
	    $std->rw_setcookie("password", $this->parent->password, $IN["remember"]);
	    $std->rw_setcookie("username", $this->parent->username, $IN["remember"]);
	    $std->rw_setcookie("userlevel", $this->parent->userlevel, $IN["remember"]);
    }
    
    function getAllUsers($select="", $exselect="", $joinExtra=false, $query="", $extraQuery="")
    {
        return NULL;
    }
}

$udbload = new vb3database();	

?>