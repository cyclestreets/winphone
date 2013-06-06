<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/engine/users.php $
 * $Revision: 50 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-12-20 20:32:52 +0000 (Sat, 20 Dec 2008) $
 *********************************************************/

define("REQUIRED_VERSION", 100);

define("k_canSearch",   1<<0);
define("k_noRestrict",  1<<1);
define("k_resetOnExpire",1<<2);
define("k_moderateAll", 1<<3);
define("k_moderateOwn", 1<<4);
define("k_acpAccess",   1<<5);
define("k_approveUL",   1<<6);
define("k_canApproveUL",1<<7);
define("k_addComments", 1<<8);
define("k_editComments",1<<9);
define("k_delComments", 1<<10);
define("k_postHTML",    1<<11);
define("k_viewOffline", 1<<12);
define("k_canChangeSkin",1<<13);
define("k_useSessionIDs",1<<14);

class user
{
	var $userdb,
		$mem_table,
		$mem_gtable;
		
	var $db_id,
		$db_name,
		$db_password,
		$db_pass_salt,
		$db_level,
		$db_email,
		$db_g_id,
		$db_g_title;

	var $reg_link,
		$admin_link;

	var $usertype;	    // Our database loader

	var $errormsg;

	var $username,
		$userlevel,
		$userid,
		$valid,
		$isAdmin,
        $isGuest,
		$langpref,
		$skinpref;
	var $moderator = array();

	var $userdetails;
	
	function user()
	{
		// nothing?
	}
	
	function initialise()
	{
		global $CONFIG, $std, $udbload;
		
		$usertype = preg_replace( "/[^a-zA-Z0-9\-\_]/", "" , $CONFIG['usertype'] );
		require_once (ROOT_PATH."/engine/userdb/{$usertype}.php");
		$this->usertype = $udbload;
		$this->usertype->init($this);
        
        if ( $this->usertype->version != REQUIRED_VERSION )
        {
            exit("Trying to load module {$usertype} but the version number does not match ".REQUIRED_VERSION);
        }

		if ( !$this->usertype->getDetails($CONFIG["userfilepath"]))
		    return FALSE;
		else
		    return TRUE;
    
	}
	
	function do_login()
	{
		global $IN, $DB, $CONFIG, $std;

		// Make sure username and password were entered
		if ( $IN["username"] == "" )
		{
			$this->errormsg .= GETLANG("er_noUsername");
			return false;
		}

		$user = trim($IN["username"]);
        $pass = $IN['userpw'];
		$name = $this->usertype->do_login($user, $pass);

		if (!$name)
        {
            $this->errormsg .= "Failed inside \$this->usertype->do_login()<br>";
		    return false;
        }
		$result2 = $DB->query("SELECT * FROM `dl_memberextra` WHERE mid={$name[$this->db_g_id]}");
		$myrow2 = $DB->fetch_row($result2);
		$result3 = $DB->query("SELECT `permissions`, `geid`, `uploadType` FROM `dl_groupsextra` WHERE geid={$name[$this->db_level]}");
		$myrow3 = $DB->fetch_row($result3);
		$result4 = $DB->query("SELECT * FROM `dl_moderators` WHERE member_id={$name[$this->db_g_id]}");
		while ($myrow4 = $DB->fetch_row($result4))
		{
			$this->moderator[] = $myrow4;
		}
        $this->userdetails = $name;
		if ($myrow2)
			$this->userdetails = array_merge($this->userdetails,$myrow2);
		if ($myrow3)
			$this->userdetails = array_merge($this->userdetails,$myrow3);
		$this->username = $this->userdetails[$this->db_name];
		$this->password = $this->userdetails[$this->db_password];
		$this->userlevel = $this->userdetails[$this->db_level];
		$this->userid = $this->userdetails[$this->db_id];
		$this->isAdmin = $this->getPermissions() & k_acpAccess;
		$this->userid = $this->userdetails[$this->db_id];
		$this->langpref = "1";
		$this->valid = true;

		return true;
	}
	
	function auto_login($userid = -1, $pass = "password")
	{
		global $DB, $IN, $CONFIG, $std;

		if ( $userid == -1 )
		{
			$this->errormsg = GETLANG("er_invaliduser");
			return false;
		}

		$myrow = $this->usertype->auto_login($userid, $pass);
		if ( !$myrow )
		{
			$this->errormsg = GETLANG("er_invaliduserpass");
			$this->valid = false;
			return false;
		}
		
		$result2 = $DB->query("SELECT * FROM `dl_memberextra` WHERE mid={$myrow[$this->db_id]}");
		$myrow2 = $DB->fetch_row($result2);
		$result3 = $DB->query("SELECT `permissions`, `geid`, `uploadType` FROM `dl_groupsextra` WHERE geid={$myrow[$this->db_level]}");
		$myrow3 = $DB->fetch_row($result3);
		$result4 = $DB->query("SELECT * FROM `dl_moderators` WHERE member_id={$myrow[$this->db_id]}");
		while ($myrow4 = $DB->fetch_row($result4))
		{
			$this->moderator[] = $myrow4;
		}
		
		$this->userdetails = $myrow;
		if ($myrow2)
			$this->userdetails = array_merge($this->userdetails,$myrow2);
		if ($myrow3)
			$this->userdetails = array_merge($this->userdetails,$myrow3);
		$this->username = $this->userdetails[$this->db_name];
		$this->password = $this->userdetails[$this->db_password];
		$this->userlevel = $this->userdetails[$this->db_level];
		$this->userid = $this->userdetails[$this->db_id];
		$this->isAdmin = $this->getPermissions() & k_acpAccess;
		$this->userid = $this->userdetails[$this->db_id];
		$this->langpref = "1";
		$this->valid = true;

		return true;
		
		
	}
	
	function login($username = "Guest User", $password = "password")
	{
		global $CONFIG, $DB, $std;
			
		$data = $this->usertype->login($username, $password);
		if ( !$data )
		{
			$this->errormsg = GETLANG("er_oldpass");
			$this->valid = false;
			return false;
		}
		
		$this->userdetails = $data;
		$this->username = $this->userdetails[$this->db_name];
		$this->userlevel = $this->userdetails[$this->db_level];
		$this->userid = $this->userdetails[$this->db_id];
		$this->isAdmin = $this->getPermissions() & k_acpAccess;
		$this->userid = $this->userdetails[$this->db_id];
		$this->langpref = "eng";
		$this->valid = true;

		return true;
		
	}

	function adminLogin($session = "")
	{
		global $DB;
		if ( $session == "" )
			return false;

		$s1 = $DB->query("SELECT * FROM dl_adminsessions WHERE sID = '$session'");
		$row1 = $DB->fetch_row($s1);
		$uid = $row1["uid"];

		$s2 = $this->userdb->query("SELECT * FROM $this->mem_table WHERE $this->db_id = $uid");
		$row2 = $this->userdb->fetch_row($s2);

		return $this->auto_login($row2[$this->db_id], $row2[$this->db_password]);
	}
	
	function uservalidate()
	{
		//return $this->usertype->uservalidate();
		global $CONFIG, $IN, $DB, $std;
		
		$uid = $std->rw_getcookie("userid");
		if ($uid)
		{
            $cuserid = $CONFIG['cookiePrefix']."userid";
            $cpwd = $CONFIG['cookiePrefix']."password";
			if (!$this->auto_login( $_COOKIE[$cuserid], $_COOKIE[$cpwd] ))
			{
				$this->username = "Guest User";
				$this->userlevel = $CONFIG["guestid"];
                $this->isGuest = TRUE;
                $this->userid = $CONFIG['guestref'];

                $result = $DB->query("SELECT `permissions`, `geid`, `uploadType` FROM `dl_groupsextra` WHERE geid={$CONFIG['guestid']}");
    			$this->userdetails = $DB->fetch_row();

				$this->errormsg = GETLANG("er_cookie");
				
				$std->rw_setcookie("userid", "0");
			    $std->rw_setcookie("password", "0");
			    $std->rw_setcookie("username", "0");
			    $std->rw_setcookie("userlevel", "0");
				
				return false;
			}
			else
			{
				if ( $CONFIG["usertype"] == USER_DEFAULT )
				{
					$ips = explode("|",$this->userdetails['iplog']);
					$updatelist = "";
					if ( $IN['ipaddr'] != $ips[0] )
					{
						$iplist = $IN['ipaddr'];
						for ( $i=0; $i<4; $i++ )
						{
							$iplist .= "|".$ips[0];
						}

						$updatelist .= " iplog='{$iplist}',";
					}
                    $DB->query("UPDATE dl_users SET {$updatelist} `lastlogin`='".time()."'  WHERE id={$this->userid}");
					return true;
				}
			}
		}
		else
		{
			$this->username = "Guest User";
			$this->userlevel = $CONFIG['guestid'];
            $result = $DB->query("SELECT * FROM `dl_groupsextra` WHERE geid={$CONFIG['guestid']}");
    		$this->userdetails = $DB->fetch_row();
            // This is really REALLY hacky
            if ( $CONFIG["usertype"] == USER_DEFAULT )
			{
                $DB->query("SELECT * FROM `dl_users` WHERE id={$CONFIG['guestref']}");
                $myrow = $DB->fetch_row();
                if ( !$this->userdetails )
                    $this->userdetails = $myrow;
                else if ( $myrow )
                    $this->userdetails = array_merge($this->userdetails, $myrow);
                $DB->query("SELECT * FROM `dl_memberextra` WHERE mid={$CONFIG['guestref']}");
                $myrow = $DB->fetch_row();
                 if ( !$this->userdetails )
                    $this->userdetails = $myrow;
                else if ( $myrow )
                    $this->userdetails = array_merge($this->userdetails, $myrow);
            }
            else
            {
                $this->userdetails[$this->db_id] = $CONFIG['guestref'];
            }
            $this->isGuest = TRUE;
            $this->userid = $CONFIG['guestref'];
			
			return false;
		}
		
		return true;
    }

    function getPermissions()
    {
        return $this->userdetails["permissions"];
    }
    function setPermission($flag)
    {
        global $DB;
        $this->userdetails["permissions"] |= ($flag);
        $update = array("permissions" => $this->userdetails["permissions"]);
        $DB->update($update, "dl_groupsextra", "`geid`='{$this->userdetails["geid"]}'");
    }
    function clearPermission($flag)
    {
        global $DB;
        $this->userdetails["permissions"] &= ~($flag);
        $update = array("permissions" => $this->userdetails["permissions"]);
        $DB->update($update, "dl_groupsextra", "`geid`='{$this->userdetails["geid"]}'");
    }
    function togglePermission($flag)
    {
        global $DB;
        $this->userdetails["permissions"] ^= ($flag);
        $update = array("permissions" => $this->userdetails["permissions"]);
        $DB->update($update, "dl_groupsextra", "`geid`='{$this->userdetails["geid"]}'");
    }

	function getAllGroups($query="", $extraQuery="")
	{
		global $CONFIG, $DB;
		$GData = array();

		if ( $CONFIG["usertype"] == USER_DEFAULT )
		{
			$result = $this->userdb->query("SELECT g.*, ge.*
											FROM `{$this->mem_gtable}` g
											LEFT JOIN `dl_groupsextra` ge ON (ge.geid=g.{$this->db_g_id}) $query");
			if ( $data = $this->userdb->fetch_row($result) )
			{
				do
				{
					$GData[$data["gid"]] = $data;
				} while ($data = $this->userdb->fetch_row($result) );
			}

		}
		else
		{
			$groups = array();
			$gextra = array();
			$result = $this->userdb->query("SELECT g.{$this->db_g_id}, g.{$this->db_g_title} FROM `{$this->mem_gtable}` g $query ORDER BY g.{$this->db_g_id}");
			if ( $data = $this->userdb->fetch_row($result) )
			{
				do
				{
					$groups[$data[$this->db_g_id]] = $data;
				} while ($data = $this->userdb->fetch_row($result) );
			}
            // Arrggghhhhh fooking stupid SMF groups
            if ( $CONFIG['usertype'] == "smf" || $CONFIG['usertype'] == "smf2" )
            {
                $groups[0][$this->db_g_id] = -1;
                $groups[0][$this->db_g_title] = "Guests";
            }
			$result = $DB->query("SELECT `permissions`, `geid`, `uploadType` FROM `dl_groupsextra` ge $extraQuery ORDER BY ge.geid");
			if ( $data = $DB->fetch_row($result) )
			{
				do
				{
                    if ( $data['geid'] == -1 )
                        $gextra[0] = $data;
                    else
                        $gextra[$data["geid"]] = $data;
				} while ($data = $DB->fetch_row($result) );
			}

			foreach ( $groups as $g )
			{
				$found = false;
				foreach ( $gextra as $ge )
				{
					if ( $g[$this->db_g_id] == $ge['geid'] )
					{
						$found = true;
						break;
					}
				}
				// Extra data not found
				if ( !$found )
				{
					$insert = array("geid" => $g[$this->db_g_id] );
					$DB->insert($insert, "dl_groupsextra");
				}
				else
				{
                    if ($g[$this->db_g_id] == -1 )  // This is BEYOND stupid
                        $g[$this->db_g_id] = 0;
					$GData[$g[$this->db_g_id]] = array_merge($groups[$g[$this->db_g_id]], $gextra[$g[$this->db_g_id]]);
				}
			}
		}
		return $GData;
	}

    // Very special case for SMF only at present. $gid is -1 so can't use arrays.
    // Good going guys.
    function getGuestGroup($gid)
    {
        $group = $this->getSingleGroup($gid);
        $group[$this->db_g_id] = $gid;
        $group[$this->db_g_title] = "Unregistered Member";
        return $group;
    }

    function getSingleGroup($gid)
	{
		global $CONFIG, $DB;

		$result = $this->userdb->query("SELECT g.{$this->db_g_id}, g.{$this->db_g_title} FROM `{$this->mem_gtable}` g WHERE g.{$this->db_g_id}={$gid} ORDER BY g.{$this->db_g_id}");
		if ( $data = $this->userdb->fetch_row($result) )
		{
			$group = $data;
		}
		$result = $DB->query("SELECT * FROM `dl_groupsextra` ge WHERE ge.geid={$gid} ORDER BY ge.geid");
		if ( $data = $DB->fetch_row($result) )
		{
			$gextra = $data;
            if ( $group )
                return array_merge($group, $gextra);
            else
                return $gextra;
		}
        else
        {
            // Extra data not found
            $insert = array("geid" => $gid );
			$DB->insert($insert, "dl_groupsextra");
            return $group;
        }
        return NULL;
	}

	function getAllUsers($select="", $exselect="", $joinExtra=false, $query="", $extraQuery="")
	{
        // IF ANYTHING CHANGES IN HERE, UPDATE THE SMF DATABASE WHICH DOES SOMETHING SLIGHTLY 
        // DIFFERENT BECAUSE OF THEIR STUPID GROUPS SYSTEM

        if ( $data = $this->usertype->getAllUsers($select, $exselect, $joinExtra, $query, $extraQuery) )
            return $data;

		global $CONFIG, $DB, $std;
		$GData = array();

		$groups = array();
		$gextra = array();

        if ( $select != "*" )
            $select = "`{$this->db_id}`, ".$select;
        if ( $exselect != "*" )
            $exselect = "mid, ".$exselect;

        $result = $this->userdb->query("SELECT $select FROM `{$this->mem_table}` $query ORDER BY `{$this->db_id}`");

		if ( $data = $this->userdb->fetch_row($result) )
		{
			do
			{
				$groups[$data[$this->db_id]] = $data;
			} while ($data = $this->userdb->fetch_row($result) );
		}
        if ( $joinExtra )
        {
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

    function getOneUser($select="", $exselect="", $joinExtra=false, $mid=-1)
	{
		global $CONFIG, $DB, $std;
		$GData = array();

		$member = array();

        if ( $select != "*" )
            $select = "{$this->db_id}, ".$select;
        if ( $exselect != "*" )
            $exselect = "mid, ".$exselect;
        $result = $this->userdb->query("SELECT $select FROM `{$this->mem_table}` WHERE `{$this->db_id}`='{$mid}'");
		$member = $this->userdb->fetch_row($result);
		if ( !$member )
            return NULL;

        if ( $joinExtra )
        {
    		$result = $DB->query("SELECT $exselect FROM `dl_memberextra` WHERE `mid`='{$mid}'");
    		$data = $DB->fetch_row($result);
    		if ( $data )
    		    return array_merge($member, $data);
            else
                return $member;
        }
        else
        {
            return $member;
        }
	}

	function updateMemberExtra($uextra, $extra)
	{
		global $CONFIG, $DB, $std;

        //if ( $CONFIG["usertype"] != USER_DEFAULT and $this->isGuest )
          //  return;

		$newUser = $this->getAllUsers("`{$this->db_level}`", "gid, dlLimitSize, limitSizePeriod, dlLimitFiles, limitFilesPeriod", true, "WHERE $uextra", "WHERE $extra" );
		$groups = $this->getAllGroups();
		
		$insert = array();
        $counter = 0;
		foreach ($newUser as $user)
		{
            if (!array_key_exists("dlLimitSize", $user))
			{
                foreach($groups as $g)
				{
					if ($user[$this->db_level] == $g[$this->db_g_id])
					{
                        $user = array_merge($user, $g);
                        $insert[$counter] = $std->updateLimits($user, 0);
                        $counter++;
                        break;
					}
				}
			}
            if ( $user[$this->db_level] != $user['gid'] )
            {
                $DB->query("UPDATE `dl_memberextra` SET `gid`='{$user[$this->db_level]}', `limitSizePeriod`='0', `limitFilesPeriod`='0' WHERE `mid`='{$user['mid']}'");
                $std->updateLimits($user, 1);
            }
		}

        $numrows = count($insert);

		if ( $numrows )
		{

			$iquery = "INSERT INTO `dl_memberextra` (`mid`, `gid`, `dlLimitSize`, `dlLimitFiles`, `limitSizePeriod`, `limitFilesPeriod`) VALUES ";
            $comma = 0;
			foreach ($insert as $i)
			{
                $comma++;
				$iquery .= "('{$i[$this->db_id]}',
                             '{$i[$this->db_level]}',
                             '{$i['dlLimitSize']}',
                             '{$i['dlLimitFiles']}',
                             '{$i['limitSizePeriod']}',
                             '{$i['limitFilesPeriod']}')";
                if ( $comma!=$numrows )
                    $iquery .= ", ";
			}
			$DB->query($iquery);
		}
	}

    function userdb_js()
    {
        return $this->usertype->userdb_js();
    }

    function userdb_loginCallback()
    {
        return $this->usertype->userdb_loginCallback();
    }

    function setusercookie()
    {
        $this->usertype->setusercookie();
    }

    function groupBox($id=0, $option="hilite", $nogroup=false, $name='group')
	{
		global $DB, $guser, $std;

		// Get groups from database
		$result = $guser->userdb->query("SELECT {$guser->db_g_id}, {$guser->db_g_title} FROM {$guser->mem_gtable} ORDER BY {$guser->db_g_title} ASC");
		if ( $myrow = $guser->userdb->fetch_row($result) )
		{
			$output = "<select name='$name'>";
            if ( $nogroup )
            {
                $output .= "<option value='-1' selected>".GETLANG("dontchange")."</option>\n";
            }
			do
			{
				if ( $option == "exclude" and $id == $myrow[$guser->db_g_id] )
					continue;

                if ( $myrow[$guser->db_g_id] != 1 and !$std->isUsingFullVersion() )
                    continue;

				$output .= "<option value=\"".$myrow[$guser->db_g_id]."\"";
				if ( $option == "hilite" and $id == $myrow[$guser->db_g_id] )
					$output .= " selected";
				$output .= ">".$myrow[$guser->db_g_title]."</option>\n";
			} while ( $myrow = $guser->userdb->fetch_row($result) );
			$output .= "</select>";
		}
        return $output;

	}
}

?>