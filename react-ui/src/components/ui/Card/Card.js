import React from 'react';
import classes from "./Card.module.css";
import {Link} from "react-router-dom";

const Card = ({category, title, id}) => {
    return (
        <Link className={classes.link} to={`/audio/${id}`}>
            <div className={classes.card}>
                <div className={classes.fill}></div>
                <div className={classes.info}>
                    <span>{category}</span>
                    <div className={classes.title}>{title}</div>
                </div>
            </div>
        </Link>
    );
};

export default Card;