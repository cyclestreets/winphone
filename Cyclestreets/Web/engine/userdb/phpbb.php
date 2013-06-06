<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/engine/userdb/phpbb.php $
 * $Revision: 50 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-12-20 20:32:52 +0000 (Sat, 20 Dec 2008) $
 *********************************************************/

//require_once ROOT_PATH."/functions/users.php";
 
class phpbb3database
{
	var $parent;
	var $name	= "phpBB 3.0.x";
    var $version = 100;
	
	function init(&$parent)
	{
		global $CONFIG;
		
		$this->parent = &$parent;
		$this->parent->mem_table = "";
		$this->parent->mem_gtable = "";

		$this->parent->db_id = "user_id";
		$this->parent->db_name = "username";
		$this->parent->db_password = "user_password";
		$this->parent->db_level = "group_id";
		$this->parent->db_email = "user_email";
		$this->parent->db_g_id = "group_id";
		$this->parent->db_g_title = "group_name";
		
		$this->parent->reg_link = $CONFIG["userfileurl"]."/ucp.php?mode=register";
		$this->parent->admin_link = $CONFIG["userfileurl"]."/adm/";

	}

	function getDetails($configpath)
	{
		global $CONFIG, $dbhost, $dbpasswd, $dbuser, $dbname, $table_prefix, $std;

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
            if ( file_exists($configpath."/config.php") )
    			require_once ($configpath."/config.php");
    		else
    		{
    			$std->error("No configuration file was found in the location specified.
    			Current user db is set to {$this->parent->mem_userdb}.
    			We were expecting to find the file at $configpath/config.php");
    			return false;
    		}

    		$dbinfo = array("sqlhost" => ($dbhost)?$dbhost:"localhost",
    						"sqlusername" => $dbuser,
    						"sqlpassword" => $dbpasswd,
    						"sqldatabase" => $dbname,
    						"sql_tbl_prefix" => $table_prefix);

    		$this->parent->userdb = new mysql($dbinfo);
    		$this->parent->mem_table = $table_prefix."users";
    		$this->parent->mem_gtable = $table_prefix."groups";
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

		echo "Should not be here";
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

$udbload = new phpbb3database();

?>