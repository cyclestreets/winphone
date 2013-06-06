<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/engine/userdb/rwd4.php $
 * $Revision: 50 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-12-20 20:32:52 +0000 (Sat, 20 Dec 2008) $
 *********************************************************/

//require_once ROOT_PATH."/functions/users.php";
 
class rwd4database
{
	var $parent;
	var $name	= "Default Database";
    var $version = 100;
	
	function init(&$parent)
	{
		global $CONFIG;
		
		$this->parent = &$parent;

		$this->parent->db_id = "id";
		$this->parent->db_g_id = "gid";
		$this->parent->db_g_title = "name";
		$this->parent->db_name = "username";
		$this->parent->db_password = "password";
		$this->parent->db_level = "group";
		$this->parent->db_email = "email";
			
		$this->parent->mem_table = "dl_users";
		$this->parent->mem_gtable = "dl_groups";

		$this->parent->reg_link = "index.php?ACT=register";
		$this->parent->admin_link = "admin.php";
	}

	function getDetails($configpath)
	{
		global $CONFIG, $DB, $std;

		$this->parent->userdb = $DB;

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
						 u.{$this->parent->db_password}, u.{$this->parent->db_email}, u.iplog
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
			
		$result = $this->parent->userdb->query(  "SELECT u.*, g.*, me.*, ge.*
					FROM `{$this->parent->mem_table}` u
					LEFT JOIN `{$this->parent->mem_gtable}` g ON (g.{$this->parent->db_g_id}=u.{$this->parent->db_level})
					LEFT JOIN `dl_memberextra` me ON (me.mid=u.{$this->parent->db_id})
					LEFT JOIN `dl_groupsextra` ge ON (ge.geid=g.{$this->parent->db_g_id})
					WHERE u.{$this->parent->db_name}='$username' AND u.{$this->parent->db_password}='$password'");
					
		if ($myrow = $this->parent->userdb->fetch_row($result))
		{
		    	return $myrow;
		}
		else
		    return false;
		
	}

	function uservalidate()
	{
		/*global $CONFIG, $IN, $DB, $std;
		
		$uid = $std->rw_getcookie("rwd_userid");
		if ($uid)
		{
			if (!$this->parent->auto_login( $_COOKIE["rwd_userid"], $_COOKIE["rwd_password"] ))
			{
				$this->parent->username = "Guest User";
				$this->parent->userlevel = $CONFIG["guestid"];
                $this->parent->isGuest = TRUE;
                $this->parent->userid = $CONFIG['guestref'];

                $result = $DB->query("SELECT * FROM `dl_groupsextra` WHERE geid={$CONFIG['guestid']}");
    			$this->parent->userdetails = $DB->fetch_row();

				$this->parent->errormsg = GETLANG("er_duffcookie");
				
				$std->rw_setcookie("rwd_userid", "0");
			    $std->rw_setcookie("rwd_password", "0");
			    $std->rw_setcookie("rwd_username", "0");
			    $std->rw_setcookie("rwd_userlevel", "0");
				
				return false;
			}
			else
			{
				if ( $CONFIG["usertype"] == USER_DEFAULT )
				{
					$ips = explode("|",$this->parent->userdetails['iplog']);
					
					if ( $IN['ipaddr'] != $ips[0] )
					{
						$iplist = $IN['ipaddr'];
						for ( $i=0; $i<4; $i++ )
						{
							$iplist .= "|".$ips[0];
						}
						$DB->query("UPDATE dl_users SET iplog='{$iplist}' WHERE id={$this->parent->userid}");
					}
					return true;
				}
			}
		}
		else
		{
			$this->parent->username = "Guest User";
			$this->parent->userlevel = $CONFIG['guestid'];
            $result = $DB->query("SELECT * FROM `dl_groupsextra` WHERE geid={$CONFIG['guestid']}");
    		$this->parent->userdetails = $DB->fetch_row();
            $this->parent->isGuest = TRUE;
            $this->parent->userid = $CONFIG['guestref'];
			return false;
		}*/
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

$udbload = new rwd4database();
	
?>