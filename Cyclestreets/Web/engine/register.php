<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/engine/register.php $
 * $Revision: 50 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-12-20 20:32:52 +0000 (Sat, 20 Dec 2008) $
 *********************************************************/

$loader = new register();

class register
{
	var $html;
	
	function register()
	{
		global $IN, $OUTPUT, $std;

        $std->AssertUsingFullVersion();
        
		$this->html = $OUTPUT->load_template("skin_register");
		
        $std->updateNav(" > ".GETLANG("nav_register"), 0);

        require_once ROOT_PATH."/engine/captcha.php";
        
		switch($IN["ACT"])
		{
			case 'register':
			$this->doit();
			break;
		}
	}

	function doit()
	{
		global $DB, $CONFIG, $IN, $guser, $rwdInfo, $std;
		if ( $IN["key"] )
		{
			$DB->query("SELECT * FROM dl_users WHERE regKey='{$IN['key']}'");
			if ($myrow = $DB->fetch_row($result))
			{
				$DB->query("UPDATE dl_users SET `group`='{$CONFIG['approvedgroup']}', `regKey`='' WHERE `regKey`='{$IN['key']}'");
                $guser->updateMemberExtra("`{$guser->db_id}`='{$myrow[$guser->db_id]}'", "`mid`='{$myrow[$guser->db_id]}'");
				$std->info( GETLANG("thankyouconfirm").$myrow["username"].". ".GETLANG("nowvalid") );
			}
			else
				$std->info( GETLANG("invalidkey") );

			return;
		}
		if ( !empty($IN["username"]) )
		{
			// LOTS of error checking
			// Check all required fields are there
			if ( $IN["password"] == "" || $IN["email"] == "" || $IN["email2"] == "" )
			{
				$std->error(GETLANG("er_missinginfo"));
				return;
			}
            if ( $CONFIG['dogdcheck']) 
            {
                // Check valid anti-spam key
                $regbot = new captcha();
                if ( !$regbot->checkValid($IN['regid']) )
                {
                    $std->error(GETLANG("er_dodgykeyentered"));
                    return;
                }                
            }
			// Confirm email matches
			if ( $IN["email"] != $IN["email2"] )
			{
				$std->error(GETLANG("er_emailmatch"));
				return;
			}
			// Confirm passwords match
			if ( $IN["password"] != $IN["password2"] )
			{
				$std->error(GETLANG("er_nomatch"));
				return;
			}
            if ( $CONFIG['doRegIPCheck'] )
            {
                // Check for duplicate username
			    $result = $DB->query("SELECT id FROM dl_users WHERE regIP='{$IN['ipaddr']}'");
                $rows = $DB->num_rows();
                if ( $rows > 0 )
                {
                    $std->error(GETLANG("er_dupRegIP"));
				    return;
                }
            }
			// Check for duplicate username
			$result = $DB->query("SELECT * FROM dl_users WHERE username='$IN[username]'");

			// If identical user found, display error
			if ($myrow = $DB->fetch_row($result))
			{
				$std->error(GETLANG("er_dupUsers"));
				return;
			}
			// Check for duplicate email unless the admin couldnt care less about this
			if (!$CONFIG['dupemail'])
			{
				$result = $DB->query("SELECT * FROM dl_users WHERE email='{$IN['email']}'");

				// If identical user found, display error
				if ($myrow = $DB->fetch_row($result))
				{
					$std->error(GETLANG("er_dupEmail"));
					return;
				}
			}
			// Check email is in a valid format
			if (!$std->emailvalidate($IN["email"]))
			{
				$std->error(GETLANG("er_invalidemail"));
				return;
			}

			$crypt = md5($IN["password"]);
			// User needs approval... from someone
			if ( $CONFIG['regconfirm'] == 1 || $CONFIG['regconfirm'] == 2 || $CONFIG['confirmboth'] == 3 )
			{
				// Randomly generate a regKey to send the user to confirm their registration
				srand(time());
				$regKey = md5(rand()+time());
				$insert = array("username" => $IN["username"],
								"password" => $crypt,
								"group" => 6,
								"email" => $IN["email"],
								"regKey" => $regKey,
                                "regDate" => time(),
                                "regIP" => $IN['ipaddr'],
                                "iplog" => $IN['ipaddr']);
				$DB->insert($insert,"dl_users");

				// If admin has to confirm their worth
				if ( $CONFIG['regconfirm'] == 2 )
				{
				    $text = str_replace( "{username}" , $IN["username"], $CONFIG['email_newuserconfirm2_msg']);
				    $text = str_replace( "{siteurl}" , $rwdInfo->url, $text);
					$std->info(GETLANG("adminapp")."<br>"."<a href='index.php?ACT=idx&sid=$sid'>".GETLANG("continue")."</a>");
				}
				else
				{
				    // User must confirm their email address
				    $link = $rwdInfo->url."/index.php?ACT=register&key=".$regKey;
				    $text = str_replace( "{username}" , $IN["username"], $CONFIG['email_newuserconfirm_msg']);
				    $text = str_replace( "{confirmlink}" , $link, $text);
				    $text = str_replace( "{siteurl}" , $rwdInfo->url, $text);
					$std->info(GETLANG("emailapp")."<br>"."<a href='index.php?ACT=idx&sid=$sid'>".GETLANG("continue")."</a>");
				}
			}
			else
			{
			    // You trusting fool....
			    $text = str_replace( "{username}" , $IN['username'], $CONFIG['email_newuser_msg']);
			    $text = str_replace( "{siteurl}" , $rwdInfo->url, $text);
			    $insert = array("username" => $IN["username"],
							    "password" => $crypt,
							    "group" => $CONFIG['approvedgroup'],
							    "email" => $IN["email"],
							    "regKey" => "");
			    $DB->insert($insert,"dl_users");
                $id = $DB->insert_id();
                $guser->updateMemberExtra("`{$guser->db_id}`='{$id}'", "`mid`='{$id}'");
				$std->info(GETLANG("newuser")."<br>"."<a href='index.php?ACT=idx&sid=$sid'>".GETLANG("continue")."</a>");

			} 
			    
			require_once(ROOT_PATH."/engine/mime/htmlMimeMail.php");
			// Send mail to the user telling them what to do next if anything
			$mail = new htmlMimeMail();
			$mail->setText($text);
			$mail->setReturnPath($CONFIG["email"]);

			$from = $CONFIG["email"];
			$mail->setFrom($from);
			$mail->setSubject("Thankyou for registering at {$CONFIG['sitename']}");
			$mail->setHeader('RW::Scripts', 'RW::Download');
			$result = $mail->send(array($IN["email"]), $CONFIG['mailtype']);
			
			// Send mail to admin
			if ( $CONFIG["email_newuser"] )
			{
				// Admin has nothing to do but is informed anyway
				if ( $CONFIG['regconfirm'] == 1 /*|| $CONFIG['confirmboth'] == 0*/ )
				{
					$text = str_replace( "{siteurl}" , $rwdInfo->url, $CONFIG['email_newuseradmin_msg']);
					$text = str_replace( "{username}" , $IN['username'], $text);
				}
				else
				{
					// WORK monkey man WORK!
					$text = str_replace( "{siteurl}" , $rwdInfo->url, $CONFIG['email_newuseradminconfirm_msg']);
					$text = str_replace( "{adminlink}" , $rwdInfo->url."/admin.php", $text);
					$text = str_replace( "{username}" , $IN['username'], $text);
				}
				
				$mail = new htmlMimeMail();
				$mail->setText($text);
				$mail->setReturnPath($CONFIG["email"]);

				$from = $IN["email"];	    // From the user
				$mail->setFrom($from);
				$mail->setSubject('RW::Scripts: A New User has registered');
				$mail->setHeader('RW::Scripts', 'RW::Download');
				$result = $mail->send(array($CONFIG["email"]), $CONFIG['mailtype']);
			}

		}
		else
		{
			$this->regForm();
		}
	}

	function regForm()
	{
		global $CONFIG, $OUTPUT;
		
        $regbot = new captcha();
		$id = $regbot->createImage();
		$data['regpost'] = "index.php?ACT=register";
		if ( $CONFIG['dogdcheck']) 
		{
			$data['regbot'] = "index.php?ACT=regbotimg&rc=$id";
			$data['regid'] = $id;
		}
		$output = $this->html->register_form($data);
		$OUTPUT->add_output($output);
	}
}
?>