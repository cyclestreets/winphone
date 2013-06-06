<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/engine/userdb/smf.php $
 * $Revision: 7 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2007-09-27 18:22:49 +0100 (Thu, 27 Sep 2007) $
 *********************************************************/

require_once ROOT_PATH."/engine/users.php";
 
class smfdatabase2
{
	var $parent;
	var $name	= "Simple Machines Forum 2.x";
    var $version = 100;
	
	function init(&$parent)
	{
		global $CONFIG;
		
		$this->parent = &$parent;
		$this->parent->mem_table = "";
		$this->parent->mem_gtable = "";

		$this->parent->db_id = "id_member";
		$this->parent->db_name = "member_name";
		$this->parent->db_password = "passwd";
		$this->parent->db_level = "id_group";
		$this->parent->db_email = "email_address";
		$this->parent->db_g_id = "id_group";
		$this->parent->db_g_title = "group_name";

		$this->parent->reg_link = $CONFIG["userfileurl"]."/index.php?action=register";
		$this->parent->admin_link = $CONFIG["userfileurl"]."/index.php?action=admin";
	}

	function getDetails($configpath)
	{
		global $CONFIG, $INFO, $std, $db_server, $db_user, $db_passwd, $db_name, $db_prefix;

        if ( $CONFIG['usermode'] == "manual" )
        {
            $dbinfo = array("sqlhost" => $CONFIG['userhost'],
    						"sqlusername" => $CONFIG['useruser'],
    						"sqlpassword" => $CONFIG['userpass'],
    						"sqldatabase" => $CONFIG['usertable'],
    						"sql_tbl_prefix" => $CONFIG['userprefix']);

    		$this->parent->userdb = new mysql($dbinfo);
    		$this->parent->mem_table = $CONFIG['userprefix']."members";
    		$this->parent->mem_gtable = $CONFIG['userprefix']."membergroups";
            return TRUE;
        }
        else
        {
    		if ( file_exists($configpath."/Settings.php") )
    			require_once ($configpath."/Settings.php");
    		else
    		{
    			$std->error("No configuration file was found in the location specified.
                            Current user db is set to {$this->name}. We were expecting to
                            find the file at {$configpath}/Settings.php");
    			return false;
    		}

    		$dbinfo = array("sqlhost" => $db_server,
    						"sqlusername" => $db_user,
    						"sqlpassword" => $db_passwd,
    						"sqldatabase" => $db_name,
    						"sql_tbl_prefix" => $db_prefix);

    		$this->parent->userdb = new mysql($dbinfo);
    		$this->parent->mem_table = $db_prefix."members";
    		$this->parent->mem_gtable = $db_prefix."membergroups";
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

        // Get user details
		$this->parent->userdb->query(  "SELECT g.{$this->parent->db_g_id}, g.{$this->parent->db_g_title},
										 u.{$this->parent->db_id},u.{$this->parent->db_name},u.{$this->parent->db_level},u.ID_POST_GROUP,
										 u.{$this->parent->db_password}, u.{$this->parent->db_email}
								FROM `{$this->parent->mem_table}` u
								LEFT JOIN `{$this->parent->mem_gtable}` g ON (g.{$this->parent->db_g_id}=u.{$this->parent->db_level})
								WHERE u.{$this->parent->db_name}='$username'");

		$name = $this->parent->userdb->fetch_row();

		// Success?
		if ( empty($name[$this->parent->db_id]) || ($name[$this->parent->db_id] == "") )
		{
			$this->parent->errormsg .= "There was an error locating the details for this user<br>";
			return false;
		}

        if ( $IN['hash_passwrd'] != sha1($name['passwd'] . $IN['sid']) )
        {
            $this->parent->errormsg .= "The password does not match the one in our records.<br>";
			return false;
		}

        if ( $name[$this->parent->db_level] == 0 )
        {
            if ( !empty($name['ID_POST_GROUP']) )
            {
                $name[$this->parent->db_level] = $name['ID_POST_GROUP'];
            }
        }
		return $name;
	}
	
	function auto_login($userid = -1, $pass = "password")
	{
        if ( !$this->parent->userdb)
        {
            $this->parent->errormsg = "No user database exists here";
            return false;
        }
		$sql = "SELECT g.{$this->parent->db_g_id}, g.{$this->parent->db_g_title},
						 u.{$this->parent->db_id},u.{$this->parent->db_name},u.{$this->parent->db_level},u.ID_POST_GROUP,
						 u.{$this->parent->db_password}, u.{$this->parent->db_email}
						FROM `{$this->parent->mem_table}` u
						LEFT JOIN `{$this->parent->mem_gtable}` g ON (g.{$this->parent->db_g_id}=u.{$this->parent->db_level})
						WHERE u.{$this->parent->db_id}='$userid' AND u.{$this->parent->db_password}='$pass'";
		// Get user details
		$this->parent->userdb->query( $sql );

		if ($myrow = $this->parent->userdb->fetch_row($result))
        {
            if ( $myrow[$this->parent->db_level] == 0 )
            {
                if ( !empty($myrow['ID_POST_GROUP']) )
                {
                    $myrow[$this->parent->db_level] = $myrow['ID_POST_GROUP'];
                }
            }
		    return $myrow;
        }
		else
		    return false;
		
	}
	
		
	function login($username, $password)
	{
		global $CONFIG, $DB;
			
		$result = $this->parent->userdb->query(  "SELECT g.{$this->parent->db_g_id}, g.{$this->parent->db_g_title},
						    u.{$this->parent->db_id},u.{$this->parent->db_name},u.{$this->parent->db_level},u.ID_POST_GROUP,
						    u.{$this->parent->db_password}, u.{$this->parent->db_email}
						    FROM `{$this->parent->mem_table}` u
						    LEFT JOIN `{$this->parent->mem_gtable}` g ON (g.{$this->parent->db_g_id}=u.{$this->parent->db_level})
						    WHERE u.{$this->parent->db_name}='$username' AND u.{$this->parent->db_password}='$password'");

		if ($myrow = $this->parent->userdb->fetch_row($result))
		{
            if ( $myrow[$this->parent->db_level] == 0 ) 
            {
                if ( !empty($myrow['ID_POST_GROUP']) )
                {
                    $myrow[$this->parent->db_level] = $myrow['ID_POST_GROUP'][0];
                }
            }
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

		if ( file_exists($CONFIG['userfilepath']."/Settings.php") )
			require_once ($CONFIG['userfilepath']."/Settings.php");
		else
		{
			$std->error("No configuration file was found in the location specified. Current user db is set to {$this->name}. We were expecting to find the file at {$configpath}/Settings.php");
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
        global $rwdInfo;
        return "<script language=\"JavaScript\" type=\"text/javascript\" src=\"{$rwdInfo->url}/engine/userdb/smf_login.js\"></script>\n
                <script language=\"JavaScript\" type=\"text/javascript\" src=\"{$rwdInfo->url}/engine/userdb/sha1.js\"></script>";
    }

    function userdb_loginCallback()
    {
        global $IN;
        return  "onsubmit=\"hashLoginPassword(this, '{$IN['sid']}');\"";
    }

    function setusercookie()
    {
        global $CONFIG, $std, $IN, $cookiename;
        $std->rw_setcookie("userid", $this->parent->userid, $IN["remember"]);
	    $std->rw_setcookie("password", $this->parent->password, $IN["remember"]);
	    $std->rw_setcookie("username", $this->parent->username, $IN["remember"]);
	    $std->rw_setcookie("userlevel", $this->parent->userlevel, $IN["remember"]);
                
        /*require ($CONFIG['userfilepath']."/Settings.php");

        // Use PHP to parse the URL, hopefully it does its job.
        $parsed_url = parse_url($CONFIG['userfileurl']);
        if (isset($parsed_url['port']))
            $parsed_url['host'] .= ':' . $parsed_url['port'];

        // Set the cookie to the forum's path only?
        if (empty($parsed_url['path']))
            $parsed_url['path'] = '';

        // This is probably very likely for apis and such, no?
        //if (!empty($smf_settings['globalCookies']))
        {
            // Try to figure out where to set the cookie; this can be confused, though.
            if (preg_match('~(?:[^\.]+\.)?(.+)\z~i', $parsed_url['host'], $parts) == 1)
                $parsed_url['host'] = '.' . $parts[1];
        }
        // If both options are off, just use no host and /.
        //elseif (empty($smf_settings['localCookies']))
        //    $parsed_url['host'] = '';

        // Get the data and path to set it on.
        $data = serialize(  array( $this->parent->userid, 
                                    $this->smf_md5_hmac($IN['hash_passwrd'], 'ys'), 
                                    time() + 3600 * 24 * 365)
                                    );

        // Set the cookie, $_COOKIE, and session variable.
        setcookie($cookiename, 
                  $data, 
                  time() + 3600 * 24 * 365, 
                  $parsed_url['path'] . '/', $parsed_url['host'], 
                  0);
        $_COOKIE[$cookiename] = $data;
        $_SESSION['login_' . $cookiename] = $data;*/
    }

    // MD5 Encryption used for passwords.
    function smf_md5_hmac($data, $key)
    {
        $key = str_pad(strlen($key) <= 64 ? $key : pack('H*', md5($key)), 64, chr(0x00));
        return md5(($key ^ str_repeat(chr(0x5c), 64)) . pack('H*', md5(($key ^ str_repeat(chr(0x36), 64)). $data)));
    }
    
    function getAllUsers($select="", $exselect="", $joinExtra=false, $query="", $extraQuery="")
    {
        if ( stristr($select, $this->parent->db_level) )
        {
            $select .= ", ID_POST_GROUP";
        }
        
        global $CONFIG, $DB, $std;
        $GData = array();

        $groups = array();
        $gextra = array();

        if ( $select != "*" )
            $select = "{$this->parent->db_id}, ".$select;
        if ( $exselect != "*" )
            $exselect = "mid, ".$exselect;

        $result = $this->parent->userdb->query("SELECT $select FROM `{$this->parent->mem_table}` $query ORDER BY `{$this->parent->db_id}`");
        if ( $data = $this->parent->userdb->fetch_row($result) )
        {
            do
            {
                if ( stristr($select, $this->parent->db_level) && $myrow[$this->parent->db_level] == 0 )
                {
                    if ( empty($data[$this->parent->db_level]) )
                    {
                        $data[$this->parent->db_level] = $data['ID_POST_GROUP'];
                    }
                }
                $groups[$data[$this->parent->db_id]] = $data;
            } while ($data = $this->parent->userdb->fetch_row($result) );
        }
        if ( $joinExtra )
        {
            $allextra = array();
            $ares = $DB->query("SELECT $exselect FROM dl_memberextra ORDER BY mid");
            while ( $data = $DB->fetch_row($ares) )
            {
                $allextra[$data['mid']] = $data;
            };
            
            $result = $DB->query("SELECT $exselect FROM `dl_memberextra` $extraQuery ORDER BY `mid`");
            if ( $data = $DB->fetch_row($result) )
            {
                do
                {
                    $gextra[$data["mid"]] = $data;
                } while ($data = $DB->fetch_row($result) );
            }

            foreach ( $groups as $i=>$v )
            {
                $found = false;
                foreach ( $gextra as $ei=>$ev )
                {
                    if ( $i == $ei )
                    {
                        $found = true;
                        break;
                    }
                }
                // Extra data not found
                if ( !$found )
                {
                    if ( !empty($allextra[$groups[$i][$this->parent->db_id]]) )
                    {
                        // found extra but group id no longer matches
                        $update = array("gid" => $groups[$i][$this->parent->db_g_id]);
                        $DB->update($update, "dl_memberextra", "mid={$groups[$i][$this->parent->db_id]}");
                        $GData[$i] = array_merge($groups[$i], $allextra[$groups[$i][$this->parent->db_id]]);
                    }
                    else
                        $GData[$i] = $groups[$i];
                }
                else
                {
                    $GData[$i] = array_merge($groups[$i], $gextra[$i]);
                }
            }
        }
        else
        {
            return $groups;
        }
        return $GData;
    }
}

$udbload = new smfdatabase2();
?>