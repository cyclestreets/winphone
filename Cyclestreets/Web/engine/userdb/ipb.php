<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/engine/userdb/ipb.php $
 * $Revision: 50 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-12-20 20:32:52 +0000 (Sat, 20 Dec 2008) $
 *********************************************************/

require_once ROOT_PATH."/engine/users.php";
 
class ipbdatabase
{
	var $parent;
	var $name	= "Invision Power Board 1.x";
    var $version = 100;

	function init(&$parent)
	{
		global $CONFIG;
		
		$this->parent = &$parent;
		$this->parent->mem_table = "";
		$this->parent->mem_gtable = "";

		$this->parent->db_id = "id";
		$this->parent->db_name = "name";
		$this->parent->db_password = "password";
		$this->parent->db_level = "mgroup";
		$this->parent->db_email = "email";
		$this->parent->db_g_id = "g_id";
		$this->parent->db_g_title = "g_title";

		$this->parent->reg_link = $CONFIG["userfileurl"]."/index.php?act=Reg&CODE=00";
		$this->parent->admin_link = $CONFIG["userfileurl"]."/admin.php";
	}

	function getDetails($configpath)
	{
		global $CONFIG, $INFO, $std;

        if ( $CONFIG['usermode'] == "manual" )
        {
            $dbinfo = array("sqlhost" => $CONFIG['userhost'],
    						"sqlusername" => $CONFIG['useruser'],
    						"sqlpassword" => $CONFIG['userpass'],
    						"sqldatabase" => $CONFIG['usertable'],
    						"sql_tbl_prefix" => $CONFIG['userprefix']);

    		$this->parent->userdb = new mysql($dbinfo);
    		$this->parent->mem_table = $CONFIG['userprefix']."members";
    		$this->parent->mem_gtable = $CONFIG['userprefix']."groups";
            return TRUE;
        }
        else
        {
      		if ( file_exists($configpath."/conf_global.php") )
    			require_once ($configpath."/conf_global.php");
    		else
    		{
    			$std->error("No configuration file was found in the location specified. Current user db is set to {$this->name}. We were expecting to find the file at {$configpath}/conf_global.php");
    			return false;
    		}
    		$dbinfo = array("sqlhost" => $INFO['sql_host'],
    						"sqlusername" => $INFO['sql_user'],
    						"sqlpassword" => $INFO['sql_pass'],
    						"sqldatabase" => $INFO['sql_database'],
    						"sql_tbl_prefix" => $INFO['sql_tbl_prefix']);

    		$this->parent->userdb = new mysql($dbinfo);
    		$this->parent->mem_table = $INFO['sql_tbl_prefix']."members";
    		$this->parent->mem_gtable = $INFO['sql_tbl_prefix']."groups";
        }
		return true;
	}
	
	function userloader($parent = NULL)
	{
		$this->parent = $parent;
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
								WHERE u.{$this->parent->db_name}='$username' AND u.{$this->parent->db_password}='$password'");

		$name = $this->parent->userdb->fetch_row();

		// Success?
		if ( empty($name[$this->parent->db_id]) || ($name[$this->parent->db_id] == "") )
		{
			$std->error("There was an error locating the details for this user");
			return false;
		}

		return $name;
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

		if ($myrow = $this->parent->userdb->fetch_row($result))
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

		if ( file_exists($CONFIG['userfilepath']."/conf_global.php") )
			require_once ($CONFIG['userfilepath']."/conf_global.php");
		else
		{
			$std->error("No configuration file was found in the location specified. Current user db is set to {$this->name}. We were expecting to find the file at {$configpath}/conf_global.php");
			return false;
		}
	
		$uid = urldecode($std->rw_getcookie($INFO['cookie_id']."member_id"));
		$passhash = urldecode($std->rw_getcookie($INFO['cookie_id']."pass_hash"));
		if ($uid)
		{
			if (!$this->auto_login( $uid, $passhash ))
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

$udbload = new ipbdatabase();	
?>