<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/index.php $
 * $Revision: 8 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-02-05 20:19:13 +0000 (Tue, 05 Feb 2008) $
 *********************************************************/
 
// Task manager not in the slightest bit influenced by invision board

class TaskManager
{
    var $date_now = array();
    var $time_now;

    function TaskManager()
    {
        $this->time_now = time();

        $this->date_now['minute']      = intval( gmdate( 'i', $this->time_now ) );
        $this->date_now['hour']        = intval( gmdate( 'H', $this->time_now ) );
        $this->date_now['wday']        = intval( gmdate( 'w', $this->time_now ) );
        $this->date_now['mday']        = intval( gmdate( 'd', $this->time_now ) );
        $this->date_now['month']       = intval( gmdate( 'm', $this->time_now ) );
        $this->date_now['year']        = intval( gmdate( 'Y', $this->time_now ) );
    }

    function RunTasks()
    {
        global $DB;

        $res = $DB->query("SELECT * FROM `dl_taskmanager`");
        while ( $task = $DB->fetch_row( $res ) )
        {
            if ( $task['nextrun'] < time() )
            {
                $this->RunThisTask($task);
                $newtime = $this->CalculateNextRunTime($task);
                $update = array("nextrun" => $newtime );
                $DB->update($update, "dl_taskmanager", "tid={$task['tid']}");
            }
        }
    }

    function RunThisTask($thistask)
    {
        global $std, $loader;
        if ( file_exists( ROOT_PATH.'/tasks/'.$thistask['script'] ) )
        {
            require_once( ROOT_PATH.'/tasks/'.$thistask['script'] );
            $loader->RunTask($thistask);
        }
        else
        {
            $std->error("Could not find task file {$thistask['script']}");
        }
    }

    function CalculateNextRunTime($task)
    {
        //-----------------------------------------
        // Did we set a day?
        //-----------------------------------------

        $day_set       = 1;
        $min_set       = 1;
        $day_increment = 0;

        $this->run_day    = $this->date_now['mday'];
        $this->run_minute = $this->date_now['minute'];
        $this->run_hour   = $this->date_now['hour'];
        $this->run_month  = $this->date_now['month'];
        $this->run_year   = $this->date_now['year'];

        if ( $task['day'] == -1 and $task['month'] == -1 )
        {
            $day_set = 0;
        }

        if ( $task['minutes'] == -1 )
        {
            $min_set = 0;
        }

        if ( $task['day'] == -1 )
        {
            if ( $task['month'] != -1 )
            {
                $this->run_day = $task['month'];
                $day_increment = 'month';
            }
            else
            {
                $this->run_day = $this->date_now['mday'];
                $day_increment = 'anyday';
            }
        }
        else
        {
            //-----------------------------------------
            // Calc. next week day from today
            //-----------------------------------------

            //$this->run_day = $this->date_now['mday'] + ( $task['day'] - $this->date_now['wday'] );
            //$day_increment = 'week';

            // Our version uses month day not week day
            $this->run_day = $task['day'];
            $day_increment = 'month';
        }

        //-----------------------------------------
        // If the date to run next is less
        // than today, best fetch the next
        // time...
        //-----------------------------------------

        if ( $this->run_day < $this->date_now['mday'] )
        {
            switch ( $day_increment )
            {
                case 'month':
                    $this->_add_month();
                    break;
                case 'week':
                    $this->_add_day(7);
                    break;
                default:
                    $this->_add_day();
                    break;
            }
        }

        //-----------------------------------------
        // Sort out the hour...
        //-----------------------------------------

        if ( $task['hour'] == -1)
        {
            $this->run_hour = $this->date_now['hour'];
        }
        else
        {
            //-----------------------------------------
            // If ! min and ! day then it's
            // every X hour
            //-----------------------------------------

            if ( ! $day_set and ! $min_set )
            {
                $this->_add_hour( $task['hour'] );
            }
            else
            {
                $this->run_hour = $task['hour'];
            }
        }

        //-----------------------------------------
        // Can we run the minute...
        //-----------------------------------------

        if ( $task['minutes'] == -1 )
        {
            $this->_add_minute();
        }
        else
        {
            if ( $task['hour'] == -1 and ! $day_set )
            {
                //-----------------------------------------
                // Runs every X minute..
                //-----------------------------------------

                $this->_add_minute($task['minutes']);
            }
            else
            {
                //-----------------------------------------
                // runs at hh:mm
                //-----------------------------------------

                $this->run_minute = $task['minutes'];
            }
        }

        if ( $this->run_hour <= $this->date_now['hour'] and $this->run_day == $this->date_now['mday'] )
        {
            if ( $task['hour'] == -1 )
            {
                //-----------------------------------------
                // Every hour...
                //-----------------------------------------

                if ( $this->run_hour == $this->date_now['hour'] and $this->run_minute <= $this->date_now['min'] )
                {
                     $this->_add_hour();
                 }
             }
             else
             {
                 //-----------------------------------------
                 // Every X hour, try again in x hours
                 //-----------------------------------------

                 if ( ! $day_set and ! $min_set )
                 {
                     $this->_add_hour($task['hour'] );
                 }

                 //-----------------------------------------
                 // Specific hour, try tomorrow
                 //-----------------------------------------

                 else if ( ! $day_set )
                 {
                     $this->_add_day();
                 }
                 else
                 {
                     //-----------------------------------------
                     // Oops, specific day...
                     //-----------------------------------------

                     switch ( $day_increment )
                    {
                        case 'month':
                            $this->_add_month();
                            break;
                        case 'week':
                            $this->_add_day(7);
                            break;
                        default:
                            $this->_add_day();
                            break;
                    }
                 }
             }
        }

        //-----------------------------------------
        // Return stamp...
        //-----------------------------------------

        $next_run = gmmktime( $this->run_hour, $this->run_minute, 0, $this->run_month, $this->run_day, $this->run_year );

        return $next_run;
    }

    /*-------------------------------------------------------------------------*/
    //
    // Add on a month for the next run time..
    //
    /*-------------------------------------------------------------------------*/

    function _add_month()
    {
        if ($this->date_now['month'] == 12)
        {
            $this->run_month = 1;
            $this->run_year++;
        }
        else
        {
            $this->run_month++;
        }
    }

    /*-------------------------------------------------------------------------*/
    //
    // Add on a day for the next run time..
    //
    /*-------------------------------------------------------------------------*/

    function _add_day($days=1)
    {
        if ( $this->date['mday'] >= ( gmdate( 't', $this->time_now ) - $days ) )
        {
            $this->run_day = ($this->date['mday'] + $days) - date( 't', $this->time_now );
            $this->_add_month();
        }
        else
        {
            $this->run_day += $days;
        }
    }

    /*-------------------------------------------------------------------------*/
    //
    // Add on a hour for the next run time...
    //
    /*-------------------------------------------------------------------------*/

    function _add_hour($hour=1)
    {
        if ($this->date_now['hour'] >= (24 - $hour ) )
        {
            $this->run_hour = ($this->date_now['hour'] + $hour) - 24;
            $this->_add_day();
        }
        else
        {
            $this->run_hour += $hour;
        }
    }

    /*-------------------------------------------------------------------------*/
    //
    // Add on a minute...
    //
    /*-------------------------------------------------------------------------*/

    function _add_minute($mins=1)
    {
        if ( $this->date_now['minute'] >= (60 - $mins) )
        {
            $this->run_minute = ( $this->date_now['minute'] + $mins ) - 60;
            $this->_add_hour();
        }
        else
        {
            $this->run_minute += $mins;
        }
    }
}

?>
